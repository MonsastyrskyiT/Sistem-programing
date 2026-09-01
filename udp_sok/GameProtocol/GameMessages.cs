namespace GameProtocol;

public static class GameActions
{
    public const string Join = "join";
    public const string Guess = "guess";
}

public sealed class GameRequest
{
    public string Action { get; set; } = string.Empty;
    public int? Number { get; set; }
}

public sealed class GameResponse
{
    public string Message { get; set; } = string.Empty;
    public bool GameOver { get; set; }
}
