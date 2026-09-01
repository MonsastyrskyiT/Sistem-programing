using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using GameProtocol;

namespace GameServer;

internal sealed class UdpGameServer
{
    private readonly UdpClient _udpClient;
    private readonly Dictionary<string, IPEndPoint> _players = new();
    private readonly int _secretNumber = Random.Shared.Next(1, 101);

    public UdpGameServer(int port)
    {
        _udpClient = new UdpClient(port);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"UDP-сервер запущено на порту {((IPEndPoint)_udpClient.Client.LocalEndPoint!).Port}.");
        Console.WriteLine("Число від 1 до 100 загадано. Сервер очікує гравців...");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult datagram = await _udpClient.ReceiveAsync(cancellationToken);
                IPEndPoint player = datagram.RemoteEndPoint;
                _players[player.ToString()] = player;

                GameRequest? request = Deserialize(datagram.Buffer);
                if (request is null)
                {
                    await SendAsync(player, "Некоректний запит.");
                    continue;
                }

                if (request.Action == GameActions.Join)
                {
                    Console.WriteLine($"Приєднався гравець {player}.");
                    await SendAsync(player, "Ви приєдналися. Вгадайте число від 1 до 100.");
                    continue;
                }

                if (request.Action != GameActions.Guess || request.Number is not >= 1 and <= 100)
                {
                    await SendAsync(player, "Спроба має бути числом від 1 до 100.");
                    continue;
                }

                int guess = request.Number.Value;
                Console.WriteLine($"Гравець {player}: {guess}");

                if (guess < _secretNumber)
                {
                    await SendAsync(player, "Занадто мало.");
                }
                else if (guess > _secretNumber)
                {
                    await SendAsync(player, "Занадто багато.");
                }
                else
                {
                    string winnerMessage = $"Гравець із {player} переміг!";
                    Console.WriteLine(winnerMessage);
                    await BroadcastWinnerAsync(winnerMessage);
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            _udpClient.Dispose();
        }
    }

    private async Task BroadcastWinnerAsync(string message)
    {
        byte[] data = Serialize(new GameResponse { Message = message, GameOver = true });

        foreach (IPEndPoint player in _players.Values)
        {
            try
            {
                await _udpClient.SendAsync(data, data.Length, player);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Не вдалося повідомити {player}: {ex.Message}");
            }
        }
    }

    private Task<int> SendAsync(IPEndPoint player, string message)
    {
        byte[] data = Serialize(new GameResponse { Message = message });
        return _udpClient.SendAsync(data, data.Length, player);
    }

    private static GameRequest? Deserialize(byte[] data)
    {
        try
        {
            return JsonSerializer.Deserialize<GameRequest>(data);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static byte[] Serialize(GameResponse response) =>
        JsonSerializer.SerializeToUtf8Bytes(response);
}
