namespace UdpColorChat;
internal sealed class ChatMessage
{
    public string Username { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public ConsoleColor Color { get; set; } = ConsoleColor.Gray;
}
