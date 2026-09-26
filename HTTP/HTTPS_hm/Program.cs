using System.Text;
using LocalHttpServer;

Console.OutputEncoding = Encoding.UTF8;
using var cancellation = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

var server = new WebServer("http://localhost:8080/");

try
{
    await server.RunAsync(cancellation.Token);
}
catch (Exception ex)
{
    Console.WriteLine($"Не вдалося запустити сервер: {ex.Message}");
}
