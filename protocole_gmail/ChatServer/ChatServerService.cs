using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using ExamChat.Protocol;

namespace ExamChat.Server;

internal sealed class ChatServerService : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly ServerStateStore _stateStore;
    private readonly ConcurrentDictionary<string, ConnectedClient> _onlineUsers =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentBag<Task> _handlers = new();
    private readonly CancellationTokenSource _stopCancellation = new();

    public ChatServerService(int port, string stateFilePath)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _stateStore = new ServerStateStore(stateFilePath);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _stopCancellation.Token);
        _listener.Start();
        Console.WriteLine($"Сервер чату запущено: {_listener.LocalEndpoint}");

        try
        {
            while (!linked.IsCancellationRequested)
            {
                TcpClient tcpClient = await _listener.AcceptTcpClientAsync(linked.Token);
                Task handler = HandleClientAsync(tcpClient, linked.Token);
                _handlers.Add(handler);
            }
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested)
        {
        }
        catch (SocketException) when (linked.IsCancellationRequested)
        {
        }
        finally
        {
            _listener.Stop();
            foreach (ConnectedClient client in _onlineUsers.Values) client.Dispose();
            await Task.WhenAll(_handlers.ToArray());
            Console.WriteLine("Сервер зупинено.");
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        var client = new ConnectedClient(tcpClient);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string? json = await client.Reader.ReadLineAsync(cancellationToken);
                if (json is null) break;

                ClientRequest? request = DeserializeRequest(json);
                if (request is null)
                {
                    await client.SendAsync(ErrorResponse(null, "Некоректний JSON-запит."));
                    continue;
                }

                await ProcessRequestAsync(client, request);
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
            if (client.Username is not null)
            {
                _onlineUsers.TryRemove(client.Username, out _);
                Console.WriteLine($"Відключився: {client.Username}");
                await BroadcastStatusAsync($"{client.Username} вийшов із мережі.");
            }

            client.Dispose();
        }
    }

    private async Task ProcessRequestAsync(ConnectedClient client, ClientRequest request)
    {
        switch (request.Type)
        {
            case RequestTypes.Register:
                await RegisterAsync(client, request);
                break;

            case RequestTypes.Login:
                await LoginAsync(client, request);
                break;

            case RequestTypes.PrivateMessage:
                await SendPrivateMessageAsync(client, request);
                break;

            case RequestTypes.CreateGroup:
                await CreateGroupAsync(client, request);
                break;

            case RequestTypes.JoinGroup:
                await JoinGroupAsync(client, request);
                break;

            case RequestTypes.GroupMessage:
                await SendGroupMessageAsync(client, request);
                break;

            case RequestTypes.ListUsers:
                await ListUsersAsync(client, request);
                break;

            case RequestTypes.ListGroups:
                await ListGroupsAsync(client, request);
                break;

            default:
                await client.SendAsync(ErrorResponse(request.RequestId, "Невідомий тип запиту."));
                break;
        }
    }

    private async Task RegisterAsync(ConnectedClient client, ClientRequest request)
    {
        bool registered = _stateStore.Register(
            request.Username ?? string.Empty,
            request.Password ?? string.Empty,
            out string error);

        await client.SendAsync(registered
            ? SuccessResponse(request.RequestId, "Реєстрацію завершено. Тепер увійдіть.")
            : ErrorResponse(request.RequestId, error));
    }

    private async Task LoginAsync(ConnectedClient client, ClientRequest request)
    {
        if (client.Username is not null)
        {
            await client.SendAsync(ErrorResponse(request.RequestId, "Ви вже увійшли."));
            return;
        }

        if (!_stateStore.Authenticate(
                request.Username ?? string.Empty,
                request.Password ?? string.Empty,
                out string displayName))
        {
            await client.SendAsync(ErrorResponse(request.RequestId, "Неправильний логін або пароль."));
            return;
        }

        if (!_onlineUsers.TryAdd(displayName, client))
        {
            await client.SendAsync(ErrorResponse(request.RequestId, "Користувач уже онлайн."));
            return;
        }

        client.Username = displayName;
        Console.WriteLine($"Увійшов: {displayName}");
        await client.SendAsync(SuccessResponse(request.RequestId, $"Вітаємо, {displayName}!"));
        await BroadcastStatusAsync($"{displayName} увійшов у мережу.");
    }

    private async Task SendPrivateMessageAsync(ConnectedClient client, ClientRequest request)
    {
        string? sender = await GetAuthenticatedUsernameAsync(client, request);
        if (sender is null) return;

        string target = request.TargetUsername?.Trim() ?? string.Empty;
        string text = request.Text?.Trim() ?? string.Empty;
        if (text.Length is < 1 or > 2000)
        {
            await client.SendAsync(ErrorResponse(request.RequestId, "Повідомлення: від 1 до 2000 символів."));
            return;
        }

        if (!_onlineUsers.TryGetValue(target, out ConnectedClient? recipient))
        {
            await client.SendAsync(ErrorResponse(request.RequestId, "Отримувач не в мережі."));
            return;
        }

        await recipient.SendAsync(new ServerMessage
        {
            Kind = MessageKinds.Event,
            EventType = EventTypes.PrivateMessage,
            Sender = sender,
            Text = text,
            SentAtUtc = DateTime.UtcNow
        });
        await client.SendAsync(SuccessResponse(request.RequestId, "Особисте повідомлення надіслано."));
    }

    private async Task CreateGroupAsync(ConnectedClient client, ClientRequest request)
    {
        string? username = await GetAuthenticatedUsernameAsync(client, request);
        if (username is null) return;

        bool created = _stateStore.CreateGroup(
            request.GroupName ?? string.Empty,
            username,
            out string error);
        await client.SendAsync(created
            ? SuccessResponse(request.RequestId, "Групу створено. Ви вже є її учасником.")
            : ErrorResponse(request.RequestId, error));
    }

    private async Task JoinGroupAsync(ConnectedClient client, ClientRequest request)
    {
        string? username = await GetAuthenticatedUsernameAsync(client, request);
        if (username is null) return;

        bool joined = _stateStore.JoinGroup(
            request.GroupName ?? string.Empty,
            username,
            out string error);
        await client.SendAsync(joined
            ? SuccessResponse(request.RequestId, "Ви приєдналися до групи.")
            : ErrorResponse(request.RequestId, error));
    }

    private async Task SendGroupMessageAsync(ConnectedClient client, ClientRequest request)
    {
        string? sender = await GetAuthenticatedUsernameAsync(client, request);
        if (sender is null) return;

        string groupName = request.GroupName?.Trim() ?? string.Empty;
        string text = request.Text?.Trim() ?? string.Empty;
        if (text.Length is < 1 or > 2000)
        {
            await client.SendAsync(ErrorResponse(request.RequestId, "Повідомлення: від 1 до 2000 символів."));
            return;
        }

        if (!_stateStore.TryGetGroupMembers(groupName, sender, out List<string> members))
        {
            await client.SendAsync(ErrorResponse(
                request.RequestId,
                "Групу не знайдено або ви не є її учасником."));
            return;
        }

        var groupEvent = new ServerMessage
        {
            Kind = MessageKinds.Event,
            EventType = EventTypes.GroupMessage,
            Sender = sender,
            GroupName = groupName,
            Text = text,
            SentAtUtc = DateTime.UtcNow
        };

        int delivered = 0;
        foreach (string member in members)
        {
            if (_onlineUsers.TryGetValue(member, out ConnectedClient? recipient))
            {
                try
                {
                    await recipient.SendAsync(groupEvent);
                    delivered++;
                }
                catch (Exception ex) when (ex is IOException or ObjectDisposedException)
                {
                }
            }
        }

        await client.SendAsync(SuccessResponse(
            request.RequestId,
            $"Повідомлення доставлено онлайн-учасникам: {delivered}."));
    }

    private async Task ListUsersAsync(ConnectedClient client, ClientRequest request)
    {
        if (await GetAuthenticatedUsernameAsync(client, request) is null) return;
        ServerMessage response = SuccessResponse(request.RequestId, "Список користувачів онлайн.");
        response.Items = _onlineUsers.Keys.OrderBy(name => name).ToList();
        await client.SendAsync(response);
    }

    private async Task ListGroupsAsync(ConnectedClient client, ClientRequest request)
    {
        if (await GetAuthenticatedUsernameAsync(client, request) is null) return;
        ServerMessage response = SuccessResponse(request.RequestId, "Список груп.");
        response.Items = _stateStore.GetGroupNames();
        await client.SendAsync(response);
    }

    private static async Task<string?> GetAuthenticatedUsernameAsync(
        ConnectedClient client,
        ClientRequest request)
    {
        if (client.Username is not null) return client.Username;

        await client.SendAsync(ErrorResponse(request.RequestId, "Спочатку виконайте вхід."));
        return null;
    }

    private async Task BroadcastStatusAsync(string text)
    {
        var statusEvent = new ServerMessage
        {
            Kind = MessageKinds.Event,
            EventType = EventTypes.UserStatus,
            Text = text,
            SentAtUtc = DateTime.UtcNow
        };

        foreach (ConnectedClient recipient in _onlineUsers.Values)
        {
            try
            {
                await recipient.SendAsync(statusEvent);
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException)
            {
            }
        }
    }

    private static ClientRequest? DeserializeRequest(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ClientRequest>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ServerMessage SuccessResponse(Guid requestId, string text) => new()
    {
        Kind = MessageKinds.Response,
        RequestId = requestId,
        Success = true,
        Text = text
    };

    private static ServerMessage ErrorResponse(Guid? requestId, string error) => new()
    {
        Kind = MessageKinds.Response,
        RequestId = requestId,
        Error = error
    };

    public ValueTask DisposeAsync()
    {
        _stopCancellation.Cancel();
        _listener.Stop();
        _stopCancellation.Dispose();
        return ValueTask.CompletedTask;
    }
}
