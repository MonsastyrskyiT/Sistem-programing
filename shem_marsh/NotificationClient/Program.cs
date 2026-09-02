using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using NotificationProtocol;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

Console.WriteLine("Клієнт системи повідомлень");
Console.Write("Адреса сервера (Enter — 127.0.0.1): ");
string host = Console.ReadLine()?.Trim() ?? string.Empty;
if (host.Length == 0) host = "127.0.0.1";

Console.Write("Порт (Enter — 5003): ");
int port = int.TryParse(Console.ReadLine(), out int enteredPort) && enteredPort is >= 1 and <= 65535
    ? enteredPort
    : 5003;

Console.Write("Ваше ім'я: ");
string username = Console.ReadLine()?.Trim() ?? string.Empty;
if (username.Length is < 1 or > 30)
{
    Console.WriteLine("Ім'я має містити від 1 до 30 символів.");
    return;
}

HashSet<NotificationCategory> subscriptions = ReadSubscriptions();

try
{
    using var client = new TcpClient();
    Console.WriteLine("Підключення...");
    await client.ConnectAsync(host, port);

    using NetworkStream stream = client.GetStream();
    using var reader = new StreamReader(stream, new UTF8Encoding(false), false, 4096, true);
    using var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true)
    {
        AutoFlush = true
    };

    await writer.WriteLineAsync(JsonSerializer.Serialize(new WireMessage
    {
        Type = MessageTypes.Subscribe,
        Username = username,
        Subscriptions = subscriptions.ToList()
    }));

    while (true)
    {
        string? json = await reader.ReadLineAsync();
        if (json is null)
        {
            Console.WriteLine("Сервер завершив з'єднання.");
            break;
        }

        WireMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<WireMessage>(json);
        }
        catch (JsonException)
        {
            Console.WriteLine("Отримано некоректну відповідь сервера.");
            continue;
        }

        if (message?.Type == MessageTypes.Acknowledgement)
        {
            Console.WriteLine(message.Text);
            Console.WriteLine("Очікування повідомлень. Ctrl+C — завершити.");
        }
        else if (message?.Type == MessageTypes.Notification && message.Category is not null)
        {
            WriteNotification(message.Category.Value, message.Text ?? string.Empty);
        }
        else if (message?.Type == MessageTypes.Error)
        {
            Console.WriteLine("Помилка сервера: " + message.Text);
        }
    }
}
catch (SocketException ex)
{
    Console.WriteLine("Помилка підключення: " + ex.Message);
}
catch (IOException ex)
{
    Console.WriteLine("Помилка обміну даними: " + ex.Message);
}

static HashSet<NotificationCategory> ReadSubscriptions()
{
    Console.WriteLine("Оберіть підписки через кому:");
    Console.WriteLine("  1 — Новина");
    Console.WriteLine("  2 — Нагадування");
    Console.WriteLine("  3 — Розважальне");
    Console.Write("Ваш вибір (наприклад, 1,3): ");

    var result = new HashSet<NotificationCategory>();
    foreach (string item in (Console.ReadLine() ?? string.Empty).Split(','))
    {
        switch (item.Trim())
        {
            case "1": result.Add(NotificationCategory.News); break;
            case "2": result.Add(NotificationCategory.Reminder); break;
            case "3": result.Add(NotificationCategory.Entertainment); break;
        }
    }

    return result;
}

static void WriteNotification(NotificationCategory category, string text)
{
    ConsoleColor oldColor = Console.ForegroundColor;
    Console.ForegroundColor = category == NotificationCategory.Emergency
        ? ConsoleColor.Red
        : ConsoleColor.Green;

    string name = category switch
    {
        NotificationCategory.News => "Новина",
        NotificationCategory.Reminder => "Нагадування",
        NotificationCategory.Entertainment => "Розважальне",
        NotificationCategory.Emergency => "ЕКСТРЕНЕ",
        _ => category.ToString()
    };

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{name}]: {text}");
    Console.ForegroundColor = oldColor;
}
