using System.Text;

namespace QuoteServer;

/// <summary>Потокобезпечний лог сервера в консоль і текстовий файл.</summary>
internal sealed class ServerLogger : IAsyncDisposable
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly StreamWriter _writer;

    public ServerLogger(string filePath)
    {
        _writer = new StreamWriter(filePath, append: true, new UTF8Encoding(false))
        {
            AutoFlush = true
        };
    }

    public async Task WriteAsync(string message)
    {
        string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
        await _writeLock.WaitAsync();
        try
        {
            Console.WriteLine(line);
            await _writer.WriteLineAsync(line);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _writeLock.WaitAsync();
        try
        {
            await _writer.DisposeAsync();
        }
        finally
        {
            _writeLock.Release();
            _writeLock.Dispose();
        }
    }
}
