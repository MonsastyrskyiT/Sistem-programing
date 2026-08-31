using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ChatProtocol;

namespace ChatServer;

internal sealed class ChatServerService
{
    private readonly TcpListener _listener;
    private readonly object _historyLock = new();
    private readonly List<ChatMessage> _history = new();
    private readonly ConcurrentDictionary<string, byte> _connectedUsers =
        new(StringComparer.OrdinalIgnoreCase);
    private long _lastMessageId;

    public ChatServerService(IPAddress address, int port)
    {
        _listener = new TcpListener(address, port);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _listener.Start();
        Console.WriteLine($"Сервер запущено: {_listener.LocalEndpoint}");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = HandleClientAsync(client, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            _listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        string? username = null;

        try
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            using (var reader = new StreamReader(stream, new UTF8Encoding(false), false, 4096, true))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true)
                   { AutoFlush = true })
            {
                string? firstLine = await reader.ReadLineAsync(cancellationToken);
                ClientRequest? connectRequest = Deserialize(firstLine);

                if (connectRequest?.Type != RequestTypes.Connect ||
                    !IsValidUsername(connectRequest.Username))
                {
                    await WriteResponseAsync(writer, Error("Некоректний юзернейм."));
                    return;
                }

                username = connectRequest.Username!.Trim();
                if (!_connectedUsers.TryAdd(username, 0))
                {
                    await WriteResponseAsync(writer, Error("Цей юзернейм уже використовується."));
                    username = null;
                    return;
                }

                AddMessage("Система", $"{username} приєднався до чату.");
                Console.WriteLine($"Підключився користувач: {username}");
                await WriteResponseAsync(writer, Success());

                while (!cancellationToken.IsCancellationRequested)
                {
                    string? line = await reader.ReadLineAsync(cancellationToken);
                    if (line is null) break;

                    ClientRequest? request = Deserialize(line);
                    ServerResponse response = request is null
                        ? Error("Некоректний формат запиту.")
                        : ProcessRequest(username, request);
                    await WriteResponseAsync(writer, response);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (IOException)
        {
        }
        catch (SocketException)
        {
        }
        finally
        {
            if (username is not null && _connectedUsers.TryRemove(username, out _))
            {
                AddMessage("Система", $"{username} залишив чат.");
                Console.WriteLine($"Відключився користувач: {username}");
            }
        }
    }

    private ServerResponse ProcessRequest(string username, ClientRequest request)
    {
        switch (request.Type)
        {
            case RequestTypes.Send:
                string message = request.Message?.Trim() ?? string.Empty;
                if (message.Length == 0) return Error("Повідомлення порожнє.");
                if (message.Length > 1000) return Error("Повідомлення задовге.");
                AddMessage(username, message);
                return Success();

            case RequestTypes.Update:
                List<ChatMessage> newMessages;
                lock (_historyLock)
                {
                    newMessages = _history
                        .Where(item => item.Id > request.LastMessageId)
                        .ToList();
                }
                return new ServerResponse { Success = true, Messages = newMessages };

            default:
                return Error("Невідомий тип запиту.");
        }
    }

    private void AddMessage(string username, string text)
    {
        lock (_historyLock)
        {
            _history.Add(new ChatMessage
            {
                Id = ++_lastMessageId,
                Username = username,
                Text = text,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }

    private static ClientRequest? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<ClientRequest>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsValidUsername(string? username) =>
        !string.IsNullOrWhiteSpace(username) && username.Trim().Length <= 30;

    private static ServerResponse Success() => new() { Success = true };
    private static ServerResponse Error(string message) => new() { Error = message };

    private static Task WriteResponseAsync(StreamWriter writer, ServerResponse response) =>
        writer.WriteLineAsync(JsonSerializer.Serialize(response));
}
