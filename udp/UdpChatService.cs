using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace UdpChat;

internal sealed class UdpChatService : IDisposable
{
    private UdpClient? _receiver;
    private CancellationTokenSource? _cancellation;
    private Task? _receiveTask;

    public bool IsRunning => _receiver is not null;

    public event EventHandler<DatagramReceivedEventArgs>? DatagramReceived;
    public event EventHandler<string>? ReceiveError;

    public void Start(int localPort)
    {
        if (IsRunning) return;

        _receiver = new UdpClient(localPort);
        _cancellation = new CancellationTokenSource();
        _receiveTask = ReceiveLoopAsync(_receiver, _cancellation.Token);
    }

    public async Task SendAsync(
        IPAddress address,
        int port,
        string sender,
        string message)
    {
        UdpClient client = _receiver ??
            throw new InvalidOperationException("Спочатку запустіть приймання повідомлень.");
        var packet = new UdpChatPacket
        {
            Sender = sender,
            Message = message,
            SentAtUtc = DateTime.UtcNow
        };
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(packet);

        await client.SendAsync(data, data.Length, new IPEndPoint(address, port));
    }

    private async Task ReceiveLoopAsync(UdpClient receiver, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult result = await receiver.ReceiveAsync(cancellationToken);
                UdpChatPacket? packet = DecodePacket(result.Buffer, result.RemoteEndPoint);
                if (packet is not null)
                {
                    DatagramReceived?.Invoke(this,
                        new DatagramReceivedEventArgs(result.RemoteEndPoint, packet));
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (SocketException ex)
        {
            ReceiveError?.Invoke(this, ex.Message);
        }
    }

    private static UdpChatPacket? DecodePacket(byte[] data, IPEndPoint remoteEndPoint)
    {
        try
        {
            UdpChatPacket? packet = JsonSerializer.Deserialize<UdpChatPacket>(data);
            if (packet is not null && !string.IsNullOrWhiteSpace(packet.Message)) return packet;
        }
        catch (JsonException)
        {
        }

        string legacyMessage = Encoding.Unicode.GetString(data).TrimEnd('\0');
        return string.IsNullOrWhiteSpace(legacyMessage)
            ? null
            : new UdpChatPacket
            {
                Sender = remoteEndPoint.ToString(),
                Message = legacyMessage,
                SentAtUtc = DateTime.UtcNow
            };
    }

    public void Dispose()
    {
        _cancellation?.Cancel();
        _receiver?.Dispose();
        _cancellation?.Dispose();
        _cancellation = null;
        _receiver = null;
        _receiveTask = null;
        GC.SuppressFinalize(this);
    }
}

internal sealed record DatagramReceivedEventArgs(
    IPEndPoint RemoteEndPoint,
    UdpChatPacket Packet);
