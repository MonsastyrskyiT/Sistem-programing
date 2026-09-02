using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using NotificationProtocol;

namespace NotificationServer;

internal sealed class NotificationServerService : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly ConcurrentDictionary<Guid, ConnectedClient> _clients = new();
    private readonly ConcurrentBag<Task> _handlers = new();
    private readonly CancellationTokenSource _stopCancellation = new();
    private Task? _acceptTask;

    public NotificationServerService(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public int ClientCount => _clients.Count;

    public void Start()
    {
        _listener.Start();
        _acceptTask = AcceptLoopAsync(_stopCancellation.Token);
        Console.WriteLine($"Сервер запущено: {_listener.LocalEndpoint}");
    }

    public async Task<int> PublishAsync(NotificationCategory category, string text)
    {
        var message = new WireMessage
        {
            Type = MessageTypes.Notification,
            Category = category,
            Text = text,
            SentAtUtc = DateTime.UtcNow
        };

        ConnectedClient[] recipients = _clients.Values
            .Where(client => category == NotificationCategory.Emergency ||
                             client.Subscriptions.Contains(category))
            .ToArray();

        int delivered = 0;
        foreach (ConnectedClient client in recipients)
        {
            try
            {
                await client.SendAsync(message);
                delivered++;
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException)
            {
                // Від'єднаний клієнт буде видалений його обробником.
            }
        }

        return delivered;
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient tcpClient = await _listener.AcceptTcpClientAsync(cancellationToken);
                _handlers.Add(HandleClientAsync(tcpClient, cancellationToken));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (SocketException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        Guid clientId = Guid.NewGuid();
        ConnectedClient? client = null;

        try
        {
            NetworkStream stream = tcpClient.GetStream();
            var reader = new StreamReader(stream, new UTF8Encoding(false), false, 4096, true);
            var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true)
            {
                AutoFlush = true
            };

            string? json = await reader.ReadLineAsync(cancellationToken);
            WireMessage? request = Deserialize(json);
            if (!TryValidateSubscription(request, out string username,
                    out HashSet<NotificationCategory> subscriptions))
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(new WireMessage
                {
                    Type = MessageTypes.Error,
                    Text = "Некоректний запит підписки."
                }));
                writer.Dispose();
                reader.Dispose();
                tcpClient.Dispose();
                return;
            }

            client = new ConnectedClient(tcpClient, reader, writer, username, subscriptions);
            _clients[clientId] = client;
            Console.WriteLine($"Підключився {username}. Підписки: {FormatSubscriptions(subscriptions)}");

            await client.SendAsync(new WireMessage
            {
                Type = MessageTypes.Acknowledgement,
                Text = "Підписку збережено."
            });

            // Клієнт нічого більше не надсилає, але читання дозволяє одразу
            // виявити його штатне або аварійне відключення.
            while (await reader.ReadLineAsync(cancellationToken) is not null)
            {
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
        }
        finally
        {
            if (_clients.TryRemove(clientId, out ConnectedClient? removed))
            {
                Console.WriteLine($"Відключився {removed.Username}.");
                removed.Dispose();
            }
            else
            {
                client?.Dispose();
                if (client is null) tcpClient.Dispose();
            }
        }
    }

    private static bool TryValidateSubscription(
        WireMessage? request,
        out string username,
        out HashSet<NotificationCategory> subscriptions)
    {
        username = request?.Username?.Trim() ?? string.Empty;
        subscriptions = request?.Subscriptions?.Where(
                category => category != NotificationCategory.Emergency && Enum.IsDefined(category))
            .ToHashSet() ?? new HashSet<NotificationCategory>();

        return request?.Type == MessageTypes.Subscribe &&
               username.Length is >= 1 and <= 30;
    }

    private static WireMessage? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<WireMessage>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string FormatSubscriptions(IEnumerable<NotificationCategory> categories)
    {
        string value = string.Join(", ", categories.Select(CategoryNames.Get));
        return value.Length == 0 ? "немає" : value;
    }

    public async ValueTask DisposeAsync()
    {
        _stopCancellation.Cancel();
        _listener.Stop();

        foreach (ConnectedClient client in _clients.Values) client.Dispose();
        if (_acceptTask is not null) await _acceptTask;
        await Task.WhenAll(_handlers.ToArray());

        _clients.Clear();
        _stopCancellation.Dispose();
    }
}

internal static class CategoryNames
{
    public static string Get(NotificationCategory category) => category switch
    {
        NotificationCategory.News => "Новина",
        NotificationCategory.Reminder => "Нагадування",
        NotificationCategory.Entertainment => "Розважальне",
        NotificationCategory.Emergency => "ЕКСТРЕНЕ",
        _ => category.ToString()
    };
}
