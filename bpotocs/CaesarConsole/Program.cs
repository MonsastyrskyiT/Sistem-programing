using CaesarConsole;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("Шифрування текстового файлу шифром Цезаря");
Console.Write("Введіть шлях до текстового файлу: ");
string? inputPath = Console.ReadLine()?.Trim().Trim('"');

if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
{
    Console.WriteLine("Помилка: файл не знайдено.");
    return;
}

Console.Write("Введіть зсув (типово 3): ");
string? shiftText = Console.ReadLine();
int shift = int.TryParse(shiftText, out int parsedShift) ? parsedShift : 3;

string directory = Path.GetDirectoryName(Path.GetFullPath(inputPath))!;
string outputPath = Path.Combine(directory,
    $"{Path.GetFileNameWithoutExtension(inputPath)}_encrypted{Path.GetExtension(inputPath)}");

using var cancellation = new CancellationTokenSource();
ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};
Console.CancelKeyPress += cancelHandler;

Console.WriteLine("Шифрування розпочато в окремому потоці.");
Console.WriteLine("Натисніть C або Ctrl+C для скасування.");

Task encryptionTask = Task.Run(
    () => CaesarCipher.EncryptFile(inputPath, outputPath, shift, cancellation.Token),
    cancellation.Token);

try
{
    while (!encryptionTask.IsCompleted)
    {
        if (!Console.IsInputRedirected && Console.KeyAvailable &&
            Console.ReadKey(true).Key == ConsoleKey.C)
        {
            cancellation.Cancel();
        }

        await Task.WhenAny(encryptionTask, Task.Delay(100));
    }

    await encryptionTask;
    Console.WriteLine($"Готово. Зашифрований файл: {outputPath}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Шифрування скасовано користувачем.");
}
catch (Exception ex)
{
    Console.WriteLine($"Помилка шифрування: {ex.Message}");
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}
