using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ExamChat.Protocol;

namespace ExamChat.Client;

internal sealed class TcpChatClient : IDisposable
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<ServerMessage>> _pending = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private TcpClient? _tcpClient;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private CancellationTokenSource? _receiveCancellation;
    private Task? _receiveTask;

    public bool IsConnected => _tcpClient?.Connected == true;

    public event EventHandler<ServerMessage>? EventReceived;
    public event EventHandler<string>? ConnectionLost;

    public async Task ConnectAsync(string host, int port)
    {
        if (IsConnected) return;

        _tcpClient = new TcpClient();
        try
        {
            await _tcpClient.ConnectAsync(host, port);
            NetworkStream stream = _tcpClient.GetStream();
            _reader = new StreamReader(stream, new UTF8Encoding(false), false, 4096, true);
            _writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true)
            {
                AutoFlush = true
            };
            _receiveCancellation = new CancellationTokenSource();
            _receiveTask = ReceiveLoopAsync(_receiveCancellation.Token);
        }
        catch
        {
            DisposeConnection();
            throw;
        }
    }

    public async Task<ServerMessage> SendRequestAsync(ClientRequest request)
    {
        StreamWriter writer = _writer ??
            throw new InvalidOperationException("Немає з'єднання із сервером.");
        var completion = new TaskCompletionSource<ServerMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_pending.TryAdd(request.RequestId, completion))
            throw new InvalidOperationException("Не вдалося зареєструвати запит.");

        try
        {
            await _writeLock.WaitAsync();
            try
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(request));
            }
            finally
            {
                _writeLock.Release();
            }

            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
        }
        finally
        {
            _pending.TryRemove(request.RequestId, out _);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string? json = await _reader!.ReadLineAsync(cancellationToken);
                if (json is null) throw new IOException("Сервер закрив з'єднання.");

                ServerMessage? message = JsonSerializer.Deserialize<ServerMessage>(json);
                if (message is null) continue;

                if (message.Kind == MessageKinds.Response &&
                    message.RequestId is Guid requestId &&
                    _pending.TryGetValue(requestId, out TaskCompletionSource<ServerMessage>? completion))
                {
                    completion.TrySetResult(message);
                }
                else if (message.Kind == MessageKinds.Event)
                {
                    EventReceived?.Invoke(this, message);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (ex is IOException or SocketException or JsonException or ObjectDisposedException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                foreach (TaskCompletionSource<ServerMessage> completion in _pending.Values)
                    completion.TrySetException(ex);
                ConnectionLost?.Invoke(this, ex.Message);
            }
        }
    }

    private void DisposeConnection()
    {
        _receiveCancellation?.Cancel();
        _writer?.Dispose();
        _reader?.Dispose();
        _tcpClient?.Dispose();
        _receiveCancellation?.Dispose();
        _writer = null;
        _reader = null;
        _tcpClient = null;
        _receiveCancellation = null;
        _receiveTask = null;
    }

    public void Dispose()
    {
        DisposeConnection();
        foreach (TaskCompletionSource<ServerMessage> completion in _pending.Values)
            completion.TrySetCanceled();
        _pending.Clear();
        _writeLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
