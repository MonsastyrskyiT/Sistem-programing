using System.Net;
using System.Net.Sockets;
using System.Text;
using UdpColorChat;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
Console.Title = "UDP Chat — кольорові повідомлення";
object consoleLock = new();

IPAddress remoteAddress = ReadIpAddress("Введіть IP отримувача: ");
int remotePort = ReadPort("Введіть порт отримувача: ");
int localPort = ReadPort("Введіть локальний порт: ");
string username = ReadUsername();
ConsoleColor ownColor = ReadColor();

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

try
{
    using var chatClient = new UdpChatClient(localPort, remoteAddress, remotePort);
    Task receiveTask = chatClient.ReceiveLoopAsync(
        (message, _) => WriteMessage(message.Username, message.Text, message.Color),
        error => WriteSystemMessage(error, ConsoleColor.Red),
        cancellation.Token);

    WriteSystemMessage(
        $"Чат запущено. Локальний порт: {localPort}. Команда /exit завершує роботу.",
        ConsoleColor.DarkGray);

    while (!cancellation.IsCancellationRequested)
    {
        WritePrompt();
        string? text;
        try
        {
            text = await Console.In.ReadLineAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }

        if (text is null || text.Equals("/exit", StringComparison.OrdinalIgnoreCase)) break;
        text = text.Trim();
        if (text.Length == 0) continue;

        if (text.Length > 4000)
        {
            WriteSystemMessage("Повідомлення не може перевищувати 4000 символів.", ConsoleColor.Red);
            continue;
        }

        await chatClient.SendAsync(new ChatMessage
        {
            Username = username,
            Text = text,
            Color = ownColor
        });

        // Відображаємо власне повідомлення тим самим кольором.
        WriteMessage(username, text, ownColor);
    }

    cancellation.Cancel();
    await receiveTask;
}
catch (SocketException ex)
{
    WriteSystemMessage("Помилка сокета: " + ex.Message, ConsoleColor.Red);
}

WriteSystemMessage("Чат завершено.", ConsoleColor.DarkGray);

void WritePrompt()
{
    lock (consoleLock)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("> ");
        Console.ResetColor();
    }
}

void WriteMessage(string username, string text, ConsoleColor color)
{
    // Чорний текст на стандартному чорному фоні був би невидимим.
    if (color == ConsoleColor.Black) color = ConsoleColor.Gray;

    lock (consoleLock)
    {
        Console.ForegroundColor = color;
        Console.WriteLine($"[{username}]: [{text}]");
        Console.ResetColor();
    }
}

void WriteSystemMessage(string text, ConsoleColor color)
{
    lock (consoleLock)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }
}

static IPAddress ReadIpAddress(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        if (IPAddress.TryParse(Console.ReadLine()?.Trim(), out IPAddress? address)) return address;
        Console.WriteLine("Некоректна IP-адреса. Спробуйте ще раз.");
    }
}

static int ReadPort(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        if (int.TryParse(Console.ReadLine(), out int port) && port is >= 1 and <= 65535)
            return port;
        Console.WriteLine("Порт має бути числом від 1 до 65535.");
    }
}

static string ReadUsername()
{
    while (true)
    {
        Console.Write("Введіть юзернейм: ");
        string username = Console.ReadLine()?.Trim() ?? string.Empty;
        if (username.Length is >= 1 and <= 30) return username;
        Console.WriteLine("Юзернейм має містити від 1 до 30 символів.");
    }
}

static ConsoleColor ReadColor()
{
    ConsoleColor[] colors =
    {
        ConsoleColor.DarkBlue,
        ConsoleColor.DarkGreen,
        ConsoleColor.DarkCyan,
        ConsoleColor.DarkRed,
        ConsoleColor.DarkMagenta,
        ConsoleColor.DarkYellow,
        ConsoleColor.Gray,
        ConsoleColor.Blue,
        ConsoleColor.Green,
        ConsoleColor.Cyan,
        ConsoleColor.Red,
        ConsoleColor.Magenta,
        ConsoleColor.Yellow,
        ConsoleColor.White
    };

    Console.WriteLine("Оберіть колір власних повідомлень:");
    for (int index = 0; index < colors.Length; index++)
    {
        Console.ForegroundColor = colors[index];
        Console.WriteLine($"{index + 1,2}. {colors[index]}");
    }
    Console.ResetColor();

    while (true)
    {
        Console.Write("Номер кольору: ");
        if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= colors.Length)
            return colors[choice - 1];
        Console.WriteLine("Оберіть номер зі списку.");
    }
}
