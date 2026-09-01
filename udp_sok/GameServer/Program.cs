using System.Text;
using GameServer;

Console.OutputEncoding = Encoding.UTF8;
const int Port = 5001;
using var cancellation = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

Console.WriteLine("Гра «Хто перший вгадає». Ctrl+C — завершити сервер.");
var server = new UdpGameServer(Port);
await server.RunAsync(cancellation.Token);
Console.WriteLine("Сервер завершив роботу.");
