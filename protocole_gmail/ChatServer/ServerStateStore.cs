using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ExamChat.Server;

internal sealed partial class ServerStateStore
{
    private readonly object _stateLock = new();
    private readonly string _filePath;
    private ServerState _state;

    public ServerStateStore(string filePath)
    {
        _filePath = filePath;
        _state = Load(filePath);
    }

    public bool Register(string username, string password, out string error)
    {
        username = username.Trim();
        if (!UsernameRegex().IsMatch(username))
        {
            error = "Юзернейм: 3–20 символів, лише літери, цифри та _.";
            return false;
        }

        if (password.Length < 4)
        {
            error = "Пароль має містити щонайменше 4 символи.";
            return false;
        }

        lock (_stateLock)
        {
            string key = Normalize(username);
            if (_state.Users.ContainsKey(key))
            {
                error = "Такий користувач уже зареєстрований.";
                return false;
            }

            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                100_000,
                HashAlgorithmName.SHA256,
                32);

            _state.Users[key] = new UserAccount
            {
                Username = username,
                Salt = Convert.ToBase64String(salt),
                PasswordHash = Convert.ToBase64String(hash)
            };
            SaveUnsafe();
        }

        error = string.Empty;
        return true;
    }

    public bool Authenticate(string username, string password, out string displayName)
    {
        lock (_stateLock)
        {
            if (!_state.Users.TryGetValue(Normalize(username), out UserAccount? account))
            {
                displayName = string.Empty;
                return false;
            }

            byte[] salt = Convert.FromBase64String(account.Salt);
            byte[] expected = Convert.FromBase64String(account.PasswordHash);
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                100_000,
                HashAlgorithmName.SHA256,
                expected.Length);

            bool valid = CryptographicOperations.FixedTimeEquals(actual, expected);
            displayName = valid ? account.Username : string.Empty;
            return valid;
        }
    }

    public bool CreateGroup(string groupName, string creator, out string error)
    {
        groupName = groupName.Trim();
        if (groupName.Length is < 2 or > 30)
        {
            error = "Назва групи має містити від 2 до 30 символів.";
            return false;
        }

        lock (_stateLock)
        {
            string key = Normalize(groupName);
            if (_state.Groups.ContainsKey(key))
            {
                error = "Група з такою назвою вже існує.";
                return false;
            }

            _state.Groups[key] = new GroupData
            {
                Name = groupName,
                Members = new List<string> { creator }
            };
            SaveUnsafe();
        }

        error = string.Empty;
        return true;
    }

    public bool JoinGroup(string groupName, string username, out string error)
    {
        lock (_stateLock)
        {
            if (!_state.Groups.TryGetValue(Normalize(groupName), out GroupData? group))
            {
                error = "Групу не знайдено.";
                return false;
            }

            if (!group.Members.Contains(username, StringComparer.OrdinalIgnoreCase))
            {
                group.Members.Add(username);
                SaveUnsafe();
            }
        }

        error = string.Empty;
        return true;
    }

    public bool TryGetGroupMembers(string groupName, string sender, out List<string> members)
    {
        lock (_stateLock)
        {
            if (!_state.Groups.TryGetValue(Normalize(groupName), out GroupData? group) ||
                !group.Members.Contains(sender, StringComparer.OrdinalIgnoreCase))
            {
                members = new List<string>();
                return false;
            }

            members = group.Members.ToList();
            return true;
        }
    }

    public List<string> GetGroupNames()
    {
        lock (_stateLock)
            return _state.Groups.Values.Select(group => group.Name).OrderBy(name => name).ToList();
    }

    private void SaveUnsafe()
    {
        string temporaryPath = _filePath + ".tmp";
        string json = JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, _filePath, true);
    }

    private static ServerState Load(string filePath)
    {
        if (!File.Exists(filePath)) return new ServerState();

        try
        {
            return JsonSerializer.Deserialize<ServerState>(File.ReadAllText(filePath)) ??
                   new ServerState();
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            return new ServerState();
        }
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    [GeneratedRegex("^[\\p{L}\\p{Nd}_]{3,20}$", RegexOptions.CultureInvariant)]
    private static partial Regex UsernameRegex();
}

internal sealed class ServerState
{
    public Dictionary<string, UserAccount> Users { get; set; } = new();
    public Dictionary<string, GroupData> Groups { get; set; } = new();
}

internal sealed class UserAccount
{
    public string Username { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}

internal sealed class GroupData
{
    public string Name { get; set; } = string.Empty;
    public List<string> Members { get; set; } = new();
}
