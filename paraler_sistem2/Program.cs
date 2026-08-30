using System.Text;
using ParalerSistem2;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

Console.WriteLine("Пошук користувачів за допомогою LINQ та Parallel LINQ");
Console.WriteLine("1 — завантажити дані з текстового файлу");
Console.WriteLine("2 — використати базу даних SQLite");
Console.Write("Оберіть джерело даних: ");
string? sourceChoice = Console.ReadLine()?.Trim();

List<UserRecord> users;

try
{
    users = sourceChoice == "2" ? LoadFromSqlite() : LoadFromTextFile();
}
catch (Exception ex)
{
    Console.WriteLine($"Помилка завантаження даних: {ex.Message}");
    return;
}

if (users.Count == 0)
{
    Console.WriteLine("Список користувачів порожній.");
    return;
}

Console.WriteLine($"Завантажено користувачів: {users.Count}");
Console.Write("Введіть частину ПІБ або юзернейму: ");
string keyword = Console.ReadLine()?.Trim() ?? string.Empty;

if (keyword.Length == 0)
{
    Console.WriteLine("Ключове слово не може бути порожнім.");
    return;
}

SearchComparison comparison = UserSearchEngine.Search(users, keyword);

Console.WriteLine();
Console.WriteLine("Знайдені користувачі:");
if (comparison.SequentialUsers.Count == 0)
{
    Console.WriteLine("Збігів не знайдено.");
}
else
{
    foreach (UserRecord user in comparison.SequentialUsers)
        Console.WriteLine(user);
}

bool resultsMatch = comparison.SequentialUsers.SequenceEqual(comparison.ParallelUsers);
Console.WriteLine();
Console.WriteLine($"LINQ:          {comparison.SequentialUsers.Count} збігів, " +
                  $"{comparison.SequentialTime.TotalMilliseconds:F4} мс " +
                  $"({comparison.SequentialTime.Ticks} тіків)");
Console.WriteLine($"Parallel LINQ: {comparison.ParallelUsers.Count} збігів, " +
                  $"{comparison.ParallelTime.TotalMilliseconds:F4} мс " +
                  $"({comparison.ParallelTime.Ticks} тіків)");
Console.WriteLine($"Результати алгоритмів збігаються: {(resultsMatch ? "так" : "ні")}");

List<UserRecord> LoadFromTextFile()
{
    Console.Write("Введіть шлях до текстового файлу: ");
    string path = ReadPath();
    if (!File.Exists(path)) throw new FileNotFoundException("Файл не знайдено.", path);

    LoadResult result = TextUserRepository.Load(path);
    if (result.SkippedLines > 0)
        Console.WriteLine($"Пропущено некоректних рядків: {result.SkippedLines}");
    return result.Users;
}

List<UserRecord> LoadFromSqlite()
{
    Console.Write("Введіть шлях до файлу бази SQLite: ");
    string databasePath = ReadPath();
    if (databasePath.Length == 0)
        throw new ArgumentException("Шлях до бази даних не вказано.");

    var repository = new SqliteUserRepository(databasePath);
    Console.Write("Шлях до текстового файлу для імпорту (Enter — пропустити): ");
    string importPath = ReadPath();

    if (importPath.Length > 0)
    {
        if (!File.Exists(importPath))
            throw new FileNotFoundException("Файл для імпорту не знайдено.", importPath);

        LoadResult importData = TextUserRepository.Load(importPath);
        int inserted = repository.Import(importData.Users);
        Console.WriteLine($"Додано записів до SQLite: {inserted}");
        if (importData.SkippedLines > 0)
            Console.WriteLine($"Пропущено некоректних рядків: {importData.SkippedLines}");
    }

    return repository.LoadAll();
}

string ReadPath() => Console.ReadLine()?.Trim().Trim('"') ?? string.Empty;
