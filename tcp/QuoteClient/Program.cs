using System.Net.Sockets;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

Console.WriteLine("Клієнт сервера «Генератор цитат»");
Console.Write("Адреса сервера (Enter — 127.0.0.1): ");
string host = Console.ReadLine()?.Trim() ?? string.Empty;
if (host.Length == 0) host = "127.0.0.1";

Console.Write("Порт (Enter — 5002): ");
int port = int.TryParse(Console.ReadLine(), out int enteredPort) && enteredPort is >= 1 and <= 65535
    ? enteredPort
    : 5002;

Console.Write("Ваше ім'я: ");
string username = Console.ReadLine()?.Trim() ?? string.Empty;
if (username.Length == 0)
{
    Console.WriteLine("Ім'я не може бути порожнім.");
    return;
}

try
{
    using var client = new TcpClient();
    Console.WriteLine("Підключення до сервера...");
    await client.ConnectAsync(host, port);

    using NetworkStream stream = client.GetStream();
    using var reader = new StreamReader(stream, new UTF8Encoding(false), false, 4096, true);
    using var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true)
    {
        AutoFlush = true
    };

    await writer.WriteLineAsync("HELLO|" + username);
    string? greeting = await reader.ReadLineAsync();
    if (greeting is null || !greeting.StartsWith("OK|", StringComparison.Ordinal))
    {
        Console.WriteLine(ReadPayload(greeting) ?? "Сервер відхилив підключення.");
        return;
    }

    Console.WriteLine(ReadPayload(greeting));

    while (true)
    {
        Console.WriteLine();
        Console.Write("Enter — отримати цитату, Q — від'єднатися: ");
        string command = Console.ReadLine()?.Trim() ?? string.Empty;

        if (command.Equals("Q", StringComparison.OrdinalIgnoreCase))
        {
            await writer.WriteLineAsync("QUIT");
            string? farewell = await reader.ReadLineAsync();
            if (farewell is not null) Console.WriteLine(ReadPayload(farewell));
            break;
        }

        await writer.WriteLineAsync("QUOTE");
        string? response = await reader.ReadLineAsync();
        if (response is null)
        {
            Console.WriteLine("Сервер закрив з'єднання.");
            break;
        }

        Console.WriteLine(response.StartsWith("QUOTE|", StringComparison.Ordinal)
            ? $"Цитата: {ReadPayload(response)}"
            : $"Помилка: {ReadPayload(response)}");
    }
}
catch (SocketException ex)
{
    Console.WriteLine($"Помилка підключення: {ex.Message}");
}
catch (IOException ex)
{
    Console.WriteLine($"Помилка обміну даними: {ex.Message}");
}

static string? ReadPayload(string? response)
{
    if (response is null) return null;
    int separator = response.IndexOf('|');
    return separator >= 0 ? response[(separator + 1)..] : response;
}
