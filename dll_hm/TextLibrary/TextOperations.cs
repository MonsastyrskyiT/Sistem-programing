using System.Text;
using System.Text.RegularExpressions;

namespace TextLibrary;

public static class TextOperations
{
    public static bool IsPalindrome(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        string normalized = string.Concat(
            text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant));

        for (int left = 0, right = normalized.Length - 1; left < right; left++, right--)
        {
            if (normalized[left] != normalized[right]) return false;
        }

        return normalized.Length > 0;
    }

    public static int CountSentences(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Regex.Split(text, @"[.!?]+")
            .Count(part => !string.IsNullOrWhiteSpace(part));
    }

    public static string Reverse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var builder = new StringBuilder(text.Length);

        for (int index = text.Length - 1; index >= 0; index--)
            builder.Append(text[index]);

        return builder.ToString();
    }
}
