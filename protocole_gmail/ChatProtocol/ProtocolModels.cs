namespace ExamChat.Protocol;

public static class RequestTypes
{
    public const string Register = "register";
    public const string Login = "login";
    public const string PrivateMessage = "private_message";
    public const string CreateGroup = "create_group";
    public const string JoinGroup = "join_group";
    public const string GroupMessage = "group_message";
    public const string ListUsers = "list_users";
    public const string ListGroups = "list_groups";
}

public static class MessageKinds
{
    public const string Response = "response";
    public const string Event = "event";
}

public static class EventTypes
{
    public const string PrivateMessage = "private_message";
    public const string GroupMessage = "group_message";
    public const string UserStatus = "user_status";
}

public sealed class ClientRequest
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? TargetUsername { get; set; }
    public string? GroupName { get; set; }
    public string? Text { get; set; }
}

public sealed class ServerMessage
{
    public string Kind { get; set; } = MessageKinds.Response;
    public Guid? RequestId { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? EventType { get; set; }
    public string? Sender { get; set; }
    public string? GroupName { get; set; }
    public string? Text { get; set; }
    public List<string> Items { get; set; } = new();
    public DateTime SentAtUtc { get; set; }
}
