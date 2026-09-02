using System.Text;
using NotificationProtocol;
using NotificationServer;

Console.OutputEncoding = Encoding.UTF8;
const int Port = 5003;
await using var server = new NotificationServerService(Port);
server.Start();

Console.WriteLine("Команди:");
Console.WriteLine("  news <текст>          — новина");
Console.WriteLine("  reminder <текст>      — нагадування");
Console.WriteLine("  entertainment <текст> — розважальне");
Console.WriteLine("  emergency <текст>     — екстрене повідомлення для всіх");
Console.WriteLine("  clients                — кількість клієнтів");
Console.WriteLine("  exit                   — завершити сервер");

while (true)
{
    Console.Write("> ");
    string input = Console.ReadLine()?.Trim() ?? string.Empty;
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    if (input.Equals("clients", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"Підключено клієнтів: {server.ClientCount}");
        continue;
    }

    if (!TryParseCommand(input, out NotificationCategory category, out string text))
    {
        Console.WriteLine("Невідома команда або порожній текст.");
        continue;
    }

    int delivered = await server.PublishAsync(category, text);
    Console.WriteLine($"Категорія «{CategoryName(category)}». Доставлено клієнтам: {delivered}.");
}

static bool TryParseCommand(
    string input,
    out NotificationCategory category,
    out string text)
{
    int separator = input.IndexOf(' ');
    string command = separator < 0 ? input : input[..separator];
    text = separator < 0 ? string.Empty : input[(separator + 1)..].Trim();

    category = command.ToLowerInvariant() switch
    {
        "news" => NotificationCategory.News,
        "reminder" => NotificationCategory.Reminder,
        "entertainment" => NotificationCategory.Entertainment,
        "emergency" => NotificationCategory.Emergency,
        _ => (NotificationCategory)(-1)
    };

    return Enum.IsDefined(category) && text.Length > 0;
}

static string CategoryName(NotificationCategory category) => category switch
{
    NotificationCategory.News => "Новина",
    NotificationCategory.Reminder => "Нагадування",
    NotificationCategory.Entertainment => "Розважальне",
    NotificationCategory.Emergency => "ЕКСТРЕНЕ",
    _ => category.ToString()
};
