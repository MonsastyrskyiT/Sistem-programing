using System.Net;
using System.Text;
using ChatServer;

Console.OutputEncoding = Encoding.UTF8;
const int Port = 5000;
using var cancellation = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

Console.WriteLine("Асинхронний TCP-сервер чату. Для зупинки натисніть Ctrl+C.");
var server = new ChatServerService(IPAddress.Any, Port);
await server.RunAsync(cancellation.Token);
