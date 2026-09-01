using System.Net.Sockets;
using System.Text.Json;
using GameProtocol;

namespace GameClient;

internal sealed class UdpGameClient : IDisposable
{
    private UdpClient? _udpClient;
    private CancellationTokenSource? _receiveCancellation;
    private Task? _receiveTask;

    public bool IsConnected => _udpClient is not null;

    public event EventHandler<GameResponse>? ResponseReceived;
    public event EventHandler<string>? ConnectionError;

    public async Task ConnectAsync(string host, int port)
    {
        DisposeConnection();
        _udpClient = new UdpClient();
        _udpClient.Connect(host, port);
        _receiveCancellation = new CancellationTokenSource();
        _receiveTask = ReceiveLoopAsync(_udpClient, _receiveCancellation.Token);
        await SendAsync(new GameRequest { Action = GameActions.Join });
    }

    public Task SendGuessAsync(int number) =>
        SendAsync(new GameRequest { Action = GameActions.Guess, Number = number });

    private async Task SendAsync(GameRequest request)
    {
        UdpClient client = _udpClient ??
            throw new InvalidOperationException("Клієнт не підключений до сервера.");
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(request);
        await client.SendAsync(data, data.Length);
    }

    private async Task ReceiveLoopAsync(UdpClient client, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult datagram = await client.ReceiveAsync(cancellationToken);
                GameResponse? response = JsonSerializer.Deserialize<GameResponse>(datagram.Buffer);
                if (response is not null)
                    ResponseReceived?.Invoke(this, response);
                if (response?.GameOver == true) return;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (ex is SocketException or JsonException or ObjectDisposedException)
        {
            if (!cancellationToken.IsCancellationRequested)
                ConnectionError?.Invoke(this, ex.Message);
        }
    }

    private void DisposeConnection()
    {
        _receiveCancellation?.Cancel();
        _udpClient?.Dispose();
        _receiveCancellation?.Dispose();
        _receiveCancellation = null;
        _udpClient = null;
        _receiveTask = null;
    }

    public void Dispose()
    {
        DisposeConnection();
        GC.SuppressFinalize(this);
    }
}
