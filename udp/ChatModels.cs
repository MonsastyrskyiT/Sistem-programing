namespace UdpChat;

internal sealed class ChatApplicationState
{
    public string Username { get; set; } = string.Empty;
    public int LocalPort { get; set; } = 6000;
    public List<ChatConversation> Chats { get; set; } = new();
}

internal sealed class ChatConversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string RemoteAddress { get; set; } = "127.0.0.1";
    public int RemotePort { get; set; }
    public List<StoredChatMessage> Messages { get; set; } = new();

    public override string ToString() => Name;
}

internal sealed class StoredChatMessage
{
    public string Sender { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsOutgoing { get; set; }
}

internal sealed class UdpChatPacket
{
    public string Sender { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
}
