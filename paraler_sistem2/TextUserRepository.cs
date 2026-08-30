namespace ParalerSistem2;

internal static class TextUserRepository
{
    public static LoadResult Load(string path)
    {
        var users = new List<UserRecord>();
        int skippedLines = 0;

        foreach (string line in File.ReadLines(path))
        {
            if (TryParse(line, out UserRecord? user))
                users.Add(user);
            else if (!string.IsNullOrWhiteSpace(line))
                skippedLines++;
        }

        return new LoadResult(users, skippedLines);
    }

    private static bool TryParse(string line, out UserRecord? user)
    {
        int separatorIndex = line.IndexOf(" - ", StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            user = null;
            return false;
        }

        string fullName = line[..separatorIndex].Trim();
        string username = line[(separatorIndex + 3)..].Trim();

        if (fullName.Length == 0 || username.Length == 0)
        {
            user = null;
            return false;
        }

        user = new UserRecord(fullName, username);
        return true;
    }
}

internal readonly record struct LoadResult(List<UserRecord> Users, int SkippedLines);
