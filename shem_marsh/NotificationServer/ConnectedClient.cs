using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using NotificationProtocol;

namespace NotificationServer;

internal sealed class ConnectedClient : IDisposable
{
    private readonly TcpClient _tcpClient;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly StreamWriter _writer;
    private int _disposed;

    public ConnectedClient(
        TcpClient tcpClient,
        StreamReader reader,
        StreamWriter writer,
        string username,
        HashSet<NotificationCategory> subscriptions)
    {
        _tcpClient = tcpClient;
        Reader = reader;
        _writer = writer;
        Username = username;
        Subscriptions = subscriptions;
    }

    public StreamReader Reader { get; }
    public string Username { get; }
    public HashSet<NotificationCategory> Subscriptions { get; }

    public async Task SendAsync(WireMessage message)
    {
        await _sendLock.WaitAsync();
        try
        {
            await _writer.WriteLineAsync(JsonSerializer.Serialize(message));
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _writer.Dispose();
        Reader.Dispose();
        _tcpClient.Dispose();
    }
}
