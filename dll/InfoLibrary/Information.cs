namespace InfoLibrary;

public static class Information
{
    public static void ShowMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Повідомлення не може бути порожнім.", nameof(message));

        Console.WriteLine($"[Інформація] {message}");
    }
}
