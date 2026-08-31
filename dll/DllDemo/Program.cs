using System.Text;
using FractionLibrary;
using InfoLibrary;
using MathLibrary;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("1");
Information.ShowMessage("Метод успішно викликано з DLL-модуля InfoLibrary.");

Console.WriteLine();
Console.WriteLine("2");
const int factorialNumber = 10;
const long checkedNumber = 29;
Console.WriteLine($"{factorialNumber}! = {NumberOperations.Factorial(factorialNumber)}");
Console.WriteLine($"Число {checkedNumber} просте: {ToUkrainian(NumberOperations.IsPrime(checkedNumber))}");
Console.WriteLine($"Число {checkedNumber} парне: {ToUkrainian(NumberOperations.IsEven(checkedNumber))}");
Console.WriteLine($"Число {checkedNumber} непарне: {ToUkrainian(NumberOperations.IsOdd(checkedNumber))}");

Console.WriteLine();
Console.WriteLine("3");
var first = new Fraction(2, 3);
var second = new Fraction(5, 6);
Console.WriteLine($"Перший дріб: {first}");
Console.WriteLine($"Другий дріб: {second}");
Console.WriteLine($"Сума: {first.Add(second)}");
Console.WriteLine($"Різниця: {first.Subtract(second)}");
Console.WriteLine($"Добуток: {first.Multiply(second)}");
Console.WriteLine($"Частка: {first.Divide(second)}");
Console.WriteLine($"Обернений до першого: {first.Reciprocal()}");
Console.WriteLine($"Перший дріб як десяткове число: {first.ToDouble():F4}");

static string ToUkrainian(bool value) => value ? "так" : "ні";
