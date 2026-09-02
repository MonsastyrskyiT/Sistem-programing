using System.Text.Json;

namespace UdpChat;

internal sealed class ChatHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public ChatHistoryStore()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UdpChatHomework");
        Directory.CreateDirectory(directory);
        FilePath = Path.Combine(directory, "chat-history.json");
    }

    public string FilePath { get; }

    public ChatApplicationState Load()
    {
        if (!File.Exists(FilePath)) return new ChatApplicationState();

        try
        {
            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<ChatApplicationState>(json) ??
                   new ChatApplicationState();
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            return new ChatApplicationState();
        }
    }

    public void Save(ChatApplicationState state)
    {
        string temporaryPath = FilePath + ".tmp";
        string json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, FilePath, true);
    }
}
