using System.Text;
using ExamChat.Server;

Console.OutputEncoding = Encoding.UTF8;
const int Port = 5004;
string statePath = Path.Combine(AppContext.BaseDirectory, "chat-server-data.json");
using var cancellation = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

Console.WriteLine("Екзаменаційний чат із виділеним сервером.");
Console.WriteLine($"Файл даних: {statePath}");
Console.WriteLine("Ctrl+C — завершити сервер.");

await using var server = new ChatServerService(Port, statePath);
await server.RunAsync(cancellation.Token);
