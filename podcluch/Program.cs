using System.Text;

Console.OutputEncoding = Encoding.UTF8;

const int NumbersCount = 1000;
const int MinimumGeneratedValue = 0;
const int MaximumGeneratedValue = 5000;

var numbers = new int[NumbersCount];
using var generationCompleted = new ManualResetEventSlim(false);
object consoleLock = new();

int maximum = 0;
int minimum = 0;
double average = 0;

void WriteLine(string message)
{
    lock (consoleLock)
    {
        Console.WriteLine(message);
    }
}

var generatorThread = new Thread(() =>
{
    WriteLine("Потік-генератор: початок генерування 1000 чисел...");

    for (int i = 0; i < numbers.Length; i++)
    {
        numbers[i] = Random.Shared.Next(
            MinimumGeneratedValue,
            MaximumGeneratedValue + 1);
    }

    WriteLine("Потік-генератор: усі числа згенеровано. Подію встановлено.");

    generationCompleted.Set();
})
{
    Name = "Generator"
};

var maximumThread = new Thread(() =>
{
    WriteLine("Потік максимуму очікує завершення генерації.");
    generationCompleted.Wait();
    WriteLine("Потік максимуму розпочав аналіз.");
    maximum = numbers.Max();
})
{
    Name = "Maximum analyzer"
};

var minimumThread = new Thread(() =>
{
    WriteLine("Потік мінімуму очікує завершення генерації.");
    generationCompleted.Wait();
    WriteLine("Потік мінімуму розпочав аналіз.");
    minimum = numbers.Min();
})
{
    Name = "Minimum analyzer"
};

var averageThread = new Thread(() =>
{
    WriteLine("Потік середнього значення очікує завершення генерації.");
    generationCompleted.Wait();
    WriteLine("Потік середнього значення розпочав аналіз.");
    average = numbers.Average();
})
{
    Name = "Average analyzer"
};

maximumThread.Start();
minimumThread.Start();
averageThread.Start();
generatorThread.Start();

generatorThread.Join();
maximumThread.Join();
minimumThread.Join();
averageThread.Join();

Console.WriteLine();
Console.WriteLine("Результати аналізу:");
Console.WriteLine($"Максимальне число: {maximum}");
Console.WriteLine($"Мінімальне число: {minimum}");
Console.WriteLine($"Середнє арифметичне: {average:F2}");
