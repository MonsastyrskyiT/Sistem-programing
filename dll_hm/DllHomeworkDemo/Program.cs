using System.Text;
using ContactValidationLibrary;
using GeometryLibrary;
using TextLibrary;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("1");
Console.WriteLine($"Площа квадрата зі стороною 5: {AreaCalculator.Square(5):F2}");
Console.WriteLine($"Площа прямокутника 4 x 7: {AreaCalculator.Rectangle(4, 7):F2}");
Console.WriteLine($"Площа трикутника з основою 8 і висотою 3: " +
                  $"{AreaCalculator.Triangle(8, 3):F2}");

Console.WriteLine();
Console.WriteLine("2");
const string palindrome = "Я несу гусеня";
const string text = "Перше речення. Друге речення! А це третє?";
Console.WriteLine($"«{palindrome}» — паліндром: {YesNo(TextOperations.IsPalindrome(palindrome))}");
Console.WriteLine($"Кількість речень: {TextOperations.CountSentences(text)}");
Console.WriteLine($"Перевернутий рядок: {TextOperations.Reverse("Привіт")}");

Console.WriteLine();
Console.WriteLine("3");
ShowValidation("ПІБ «Іваненко Іван Іванович»",
    ContactValidator.IsValidFullName("Іваненко Іван Іванович"));
ShowValidation("ПІБ «Іваненко-Петренко Іван»",
    ContactValidator.IsValidFullName("Іваненко-Петренко Іван"));
ShowValidation("Вік «25»", ContactValidator.IsValidAge("25"));
ShowValidation("Вік «25 років»", ContactValidator.IsValidAge("25 років"));
ShowValidation("Телефон «+380501234567»", ContactValidator.IsValidPhone("+380501234567"));
ShowValidation("Email «student@example.com»",
    ContactValidator.IsValidEmail("student@example.com"));

static void ShowValidation(string field, bool isValid) =>
    Console.WriteLine($"{field}: {(isValid ? "коректно" : "некоректно")}");

static string YesNo(bool value) => value ? "так" : "ні";
