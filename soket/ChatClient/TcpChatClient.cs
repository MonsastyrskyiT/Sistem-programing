using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ChatProtocol;

namespace ChatClient;

internal sealed class TcpChatClient : IDisposable
{
    private readonly SemaphoreSlim _requestLock = new(1, 1);
    private TcpClient? _client;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public bool IsConnected => _client?.Connected == true;

    public async Task ConnectAsync(string host, int port, string username)
    {
        DisposeConnection();
        _client = new TcpClient();

        try
        {
            await _client.ConnectAsync(host, port);
            NetworkStream stream = _client.GetStream();
            _reader = new StreamReader(stream, new UTF8Encoding(false), false, 4096, true);
            _writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true)
            {
                AutoFlush = true
            };

            ServerResponse response = await SendRequestAsync(new ClientRequest
            {
                Type = RequestTypes.Connect,
                Username = username
            });
            EnsureSuccess(response);
        }
        catch
        {
            DisposeConnection();
            throw;
        }
    }

    public async Task SendMessageAsync(string message)
    {
        ServerResponse response = await SendRequestAsync(new ClientRequest
        {
            Type = RequestTypes.Send,
            Message = message
        });
        EnsureSuccess(response);
    }

    public async Task<List<ChatMessage>> GetNewMessagesAsync(long lastMessageId)
    {
        ServerResponse response = await SendRequestAsync(new ClientRequest
        {
            Type = RequestTypes.Update,
            LastMessageId = lastMessageId
        });
        EnsureSuccess(response);
        return response.Messages;
    }

    private async Task<ServerResponse> SendRequestAsync(ClientRequest request)
    {
        if (_reader is null || _writer is null)
            throw new InvalidOperationException("Клієнт не підключений до сервера.");

        await _requestLock.WaitAsync();
        try
        {
            await _writer.WriteLineAsync(JsonSerializer.Serialize(request));
            string? json = await _reader.ReadLineAsync();
            if (json is null) throw new IOException("Сервер закрив з'єднання.");

            return JsonSerializer.Deserialize<ServerResponse>(json)
                   ?? throw new IOException("Сервер повернув некоректну відповідь.");
        }
        finally
        {
            _requestLock.Release();
        }
    }

    private static void EnsureSuccess(ServerResponse response)
    {
        if (!response.Success)
            throw new InvalidOperationException(response.Error ?? "Сервер відхилив запит.");
    }

    private void DisposeConnection()
    {
        _writer?.Dispose();
        _reader?.Dispose();
        _client?.Dispose();
        _writer = null;
        _reader = null;
        _client = null;
    }

    public void Dispose()
    {
        DisposeConnection();
        _requestLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
