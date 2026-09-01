using System.Net;
using System.Net.Sockets;
using System.Text;

namespace QuoteServer;

internal sealed class QuoteServerService
{
    private static readonly string[] Quotes =
    {
        "Знання — це сила.",
        "Єдиний спосіб зробити видатну роботу — любити те, що робиш.",
        "Успіх — це сума невеликих зусиль, повторюваних щодня.",
        "Майбутнє залежить від того, що ти робиш сьогодні.",
        "Не бійся рухатися повільно, бійся стояти на місці.",
        "Досвід — це ім'я, яке кожен дає своїм помилкам.",
        "Складні дороги часто ведуть до прекрасних місць.",
        "Навчання ніколи не вичерпує розум.",
        "Мрія стає метою, коли зроблено перший крок.",
        "Якість — це робити правильно, навіть коли ніхто не дивиться."
    };

    private readonly TcpListener _listener;
    private readonly ServerLogger _logger;

    public QuoteServerService(IPAddress address, int port, ServerLogger logger)
    {
        _listener = new TcpListener(address, port);
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var clientTasks = new List<Task>();
        _listener.Start();
        await _logger.WriteAsync($"Сервер запущено на {_listener.LocalEndpoint}.");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken);
                clientTasks.Add(HandleClientAsync(client, cancellationToken));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            _listener.Stop();
            await Task.WhenAll(clientTasks);
            await _logger.WriteAsync("Сервер зупинено.");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        string endpoint = client.Client.RemoteEndPoint?.ToString() ?? "невідома адреса";
        string? username = null;

        try
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            using (var reader = new StreamReader(stream, new UTF8Encoding(false), false, 4096, true))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true)
                   { AutoFlush = true })
            {
                string? greeting = await reader.ReadLineAsync(cancellationToken);
                if (!TryReadUsername(greeting, out username))
                {
                    await writer.WriteLineAsync("ERROR|Некоректне ім'я користувача.");
                    return;
                }

                await _logger.WriteAsync($"Підключився: {username} ({endpoint}).");
                await writer.WriteLineAsync("OK|Підключення встановлено.");

                while (!cancellationToken.IsCancellationRequested)
                {
                    string? command = await reader.ReadLineAsync(cancellationToken);
                    if (command is null || command == "QUIT") break;

                    if (command == "QUOTE")
                    {
                        string quote = Quotes[Random.Shared.Next(Quotes.Length)];
                        await writer.WriteLineAsync("QUOTE|" + quote);
                        await _logger.WriteAsync(
                            $"Запит цитати від {username} ({endpoint}). Надіслано: «{quote}»");
                    }
                    else
                    {
                        await writer.WriteLineAsync("ERROR|Невідома команда.");
                    }
                }

                if (!cancellationToken.IsCancellationRequested)
                    await writer.WriteLineAsync("BYE|До побачення!");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (IOException ex)
        {
            await _logger.WriteAsync($"З'єднання з {username ?? endpoint} перервано: {ex.Message}");
        }
        catch (SocketException ex)
        {
            await _logger.WriteAsync($"Помилка сокета {username ?? endpoint}: {ex.Message}");
        }
        finally
        {
            if (username is not null)
                await _logger.WriteAsync($"Відключився: {username} ({endpoint}).");
        }
    }

    private static bool TryReadUsername(string? greeting, out string? username)
    {
        const string prefix = "HELLO|";
        username = null;

        if (greeting is null || !greeting.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        string value = greeting[prefix.Length..].Trim();
        if (value.Length is < 1 or > 30 || value.Contains('|')) return false;

        username = value;
        return true;
    }
}
