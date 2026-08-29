using System.Text;

namespace CaesarWinForms;

internal static class CaesarCipher
{
    private const string UkrainianLower = "абвгґдеєжзиіїйклмнопрстуфхцчшщьюя";
    private const string UkrainianUpper = "АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯ";

    public static void EncryptFile(string inputPath, string outputPath, int shift,
        CancellationToken token, IProgress<int>? progress = null)
    {
        string tempPath = outputPath + ".tmp";
        long total = new FileInfo(inputPath).Length;
        long processed = 0;

        try
        {
            using var reader = new StreamReader(inputPath, Encoding.UTF8, true);
            using var writer = new StreamWriter(tempPath, false, new UTF8Encoding(false));
            var buffer = new char[8192];
            int count;

            while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                token.ThrowIfCancellationRequested();
                for (int i = 0; i < count; i++) buffer[i] = Encrypt(buffer[i], shift);
                writer.Write(buffer, 0, count);
                processed += count * 2L;
                progress?.Report(total == 0 ? 100 : (int)Math.Min(99, processed * 100 / total));
            }

            token.ThrowIfCancellationRequested();
            writer.Close();
            File.Move(tempPath, outputPath, true);
            progress?.Report(100);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static char Encrypt(char value, int shift)
    {
        if (value is >= 'a' and <= 'z') return Shift(value, 'a', 26, shift);
        if (value is >= 'A' and <= 'Z') return Shift(value, 'A', 26, shift);
        foreach (string alphabet in new[] { UkrainianLower, UkrainianUpper })
        {
            int index = alphabet.IndexOf(value);
            if (index >= 0)
            {
                int normalized = ((shift % alphabet.Length) + alphabet.Length) % alphabet.Length;
                return alphabet[(index + normalized) % alphabet.Length];
            }
        }
        return value;
    }

    private static char Shift(char value, char first, int size, int shift)
    {
        int normalized = ((shift % size) + size) % size;
        return (char)(first + (value - first + normalized) % size);
    }
}
