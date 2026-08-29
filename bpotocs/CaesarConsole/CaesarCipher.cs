using System.Text;

namespace CaesarConsole;

internal static class CaesarCipher
{
    private const string UkrainianLower = "абвгґдеєжзиіїйклмнопрстуфхцчшщьюя";
    private const string UkrainianUpper = "АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯ";

    public static char Encrypt(char value, int shift)
    {
        if (value is >= 'a' and <= 'z') return Shift(value, 'a', 26, shift);
        if (value is >= 'A' and <= 'Z') return Shift(value, 'A', 26, shift);

        char result = ShiftInAlphabet(value, UkrainianLower, shift);
        if (result != value) return result;
        return ShiftInAlphabet(value, UkrainianUpper, shift);
    }

    public static void EncryptFile(string inputPath, string outputPath, int shift,
        CancellationToken cancellationToken)
    {
        string tempPath = outputPath + ".tmp";

        try
        {
            using var reader = new StreamReader(inputPath, Encoding.UTF8, true);
            using var writer = new StreamWriter(tempPath, false, new UTF8Encoding(false));
            var buffer = new char[8192];

            int count;
            while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (int i = 0; i < count; i++)
                    buffer[i] = Encrypt(buffer[i], shift);
                writer.Write(buffer, 0, count);
            }

            cancellationToken.ThrowIfCancellationRequested();
            writer.Close();
            File.Move(tempPath, outputPath, true);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static char Shift(char value, char first, int alphabetSize, int shift)
    {
        int normalizedShift = ((shift % alphabetSize) + alphabetSize) % alphabetSize;
        return (char)(first + (value - first + normalizedShift) % alphabetSize);
    }

    private static char ShiftInAlphabet(char value, string alphabet, int shift)
    {
        int index = alphabet.IndexOf(value);
        if (index < 0) return value;
        int normalizedShift = ((shift % alphabet.Length) + alphabet.Length) % alphabet.Length;
        return alphabet[(index + normalizedShift) % alphabet.Length];
    }
}
