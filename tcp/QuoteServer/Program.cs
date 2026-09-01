using System.Net;
using System.Text;
using QuoteServer;

Console.OutputEncoding = Encoding.UTF8;
const int Port = 5002;
string logPath = Path.Combine(AppContext.BaseDirectory, "quote-server.log");
using var cancellation = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

Console.WriteLine("Сервер «Генератор цитат». Ctrl+C — завершити роботу.");
Console.WriteLine($"Файл журналу: {logPath}");

await using var logger = new ServerLogger(logPath);
var server = new QuoteServerService(IPAddress.Any, Port, logger);
await server.RunAsync(cancellation.Token);
