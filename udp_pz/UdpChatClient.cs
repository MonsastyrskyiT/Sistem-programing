using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace UdpColorChat;

internal sealed class UdpChatClient : IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _remoteEndPoint;

    public UdpChatClient(int localPort, IPAddress remoteAddress, int remotePort)
    {
        _udpClient = new UdpClient(localPort);
        _remoteEndPoint = new IPEndPoint(remoteAddress, remotePort);
    }

    public async Task SendAsync(ChatMessage message)
    {
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(message);
        await _udpClient.SendAsync(data, data.Length, _remoteEndPoint);
    }

    public async Task ReceiveLoopAsync(
        Action<ChatMessage, IPEndPoint> messageReceived,
        Action<string> errorReceived,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult result = await _udpClient.ReceiveAsync(cancellationToken);

                try
                {
                    ChatMessage? message = JsonSerializer.Deserialize<ChatMessage>(result.Buffer);
                    if (IsValid(message))
                        messageReceived(message!, result.RemoteEndPoint);
                    else
                        errorReceived($"Отримано некоректне повідомлення від {result.RemoteEndPoint}.");
                }
                catch (JsonException)
                {
                    errorReceived($"Отримано невідомий формат даних від {result.RemoteEndPoint}.");
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
            errorReceived("Помилка сокета: " + ex.Message);
        }
    }

    private static bool IsValid(ChatMessage? message) =>
        message is not null &&
        !string.IsNullOrWhiteSpace(message.Username) &&
        !string.IsNullOrWhiteSpace(message.Text) &&
        Enum.IsDefined(message.Color);

    public void Dispose()
    {
        _udpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
