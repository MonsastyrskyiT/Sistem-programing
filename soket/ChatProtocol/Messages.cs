namespace ChatProtocol;

public static class RequestTypes
{
    public const string Connect = "connect";
    public const string Send = "send";
    public const string Update = "update";
}

public sealed class ClientRequest
{
    public string Type { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Message { get; set; }
    public long LastMessageId { get; set; }
}

public sealed class ServerResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<ChatMessage> Messages { get; set; } = new();
}

public sealed class ChatMessage
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
