using System.Net.Mail;
using System.Text.RegularExpressions;

namespace ContactValidationLibrary;

public static partial class ContactValidator
{
    public static bool IsValidFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName != fullName.Trim())
            return false;

        string[] parts = fullName.Split(' ');
        return parts.Length >= 2 &&
               parts.All(part => part.Length > 0 && part.All(char.IsLetter));
    }

    public static bool IsValidAge(string? age) =>
        !string.IsNullOrEmpty(age) && age.All(character => character is >= '0' and <= '9');

    public static bool IsValidPhone(string? phone) =>
        phone is not null && UkrainianPhoneRegex().IsMatch(phone);

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email != email.Trim()) return false;

        try
        {
            var address = new MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase) &&
                   email.Length <= 254;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"^\+380\d{9}$", RegexOptions.CultureInvariant)]
    private static partial Regex UkrainianPhoneRegex();
}
