namespace NotificationProtocol;

public enum NotificationCategory
{
    News,
    Reminder,
    Entertainment,
    Emergency
}

public static class MessageTypes
{
    public const string Subscribe = "subscribe";
    public const string Acknowledgement = "ack";
    public const string Notification = "notification";
    public const string Error = "error";
}

public sealed class WireMessage
{
    public string Type { get; set; } = string.Empty;
    public string? Username { get; set; }
    public List<NotificationCategory> Subscriptions { get; set; } = new();
    public NotificationCategory? Category { get; set; }
    public string? Text { get; set; }
    public DateTime SentAtUtc { get; set; }
}
