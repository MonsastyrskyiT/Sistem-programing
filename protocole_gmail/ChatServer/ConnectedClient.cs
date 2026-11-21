using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ExamChat.Protocol;

namespace ExamChat.Server;

internal sealed class ConnectedClient : IDisposable
{
    private readonly TcpClient _tcpClient;
    private readonly StreamWriter _writer;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private int _disposed;

    public ConnectedClient(TcpClient tcpClient)
    {
        _tcpClient = tcpClient;
        NetworkStream stream = tcpClient.GetStream();
        Reader = new StreamReader(stream, new UTF8Encoding(false), false, 4096, true);
        _writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true)
        {
            AutoFlush = true
        };
    }

    public StreamReader Reader { get; }
    public string? Username { get; set; }

    public async Task SendAsync(ServerMessage message)
    {
        await _writeLock.WaitAsync();
        try
        {
            await _writer.WriteLineAsync(JsonSerializer.Serialize(message));
        }
        finally
        {
            _writeLock.Release();
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
