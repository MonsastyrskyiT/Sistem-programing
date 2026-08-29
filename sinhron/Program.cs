using System.Text;
using Sinhron;

Console.OutputEncoding = Encoding.UTF8;
object consoleLock = new();

void Print(string message)
{
    lock (consoleLock)
    {
        Console.WriteLine(message);
    }
}

RunBankAccountTask();
Console.WriteLine();
RunSemaphoreTask();

void RunBankAccountTask()
{
    const int workersCount = 6;
    const int operationsPerWorker = 8;
    var account = new BankAccount(1000m);
    var workers = new Thread[workersCount];

    Consol.WriteLine($"Початковий баланс: {account.Balance:F2} грн");

    for (int workerNumber = 1; workerNumber <= workers.Length; workerNumber++)
    {
        int capturedNumber = workerNumber;
        workers[workerNumber - 1] = new Thread(() =>
        {
            for (int operation = 1; operation <= operationsPerWorker; operation++)
            {
                decimal requestedAmount = Random.Shared.Next(1, 301);

                if (Random.Shared.Next(2) == 0)
                {
                    TransactionResult result = account.Deposit(requestedAmount);
                    Print($"Потік {capturedNumber} [ID {Environment.CurrentManagedThreadId}] " +
                          $"додав {result.Amount:F2} грн. Баланс: {result.NewBalance:F2} грн");
                }
                else
                {
                    TransactionResult result = account.Withdraw(requestedAmount);
                    Print($"Потік {capturedNumber} [ID {Environment.CurrentManagedThreadId}] " +
                          $"зняв {result.Amount:F2} грн. Баланс: {result.NewBalance:F2} грн");
                }

                Thread.Sleep(Random.Shared.Next(10, 41));
            }
        })
        {
            Name = $"Bank worker {capturedNumber}"
        };
    }

    foreach (Thread worker in workers) worker.Start();
    foreach (Thread worker in workers) worker.Join();

    Print($"Усі операції завершено. Підсумковий баланс: {account.Balance:F2} грн");
}

void RunSemaphoreTask()
{
    const int threadsCount = 10;
    const int numbersPerThread = 8;
    using var semaphore = new SemaphoreSlim(3, 3);
    var threads = new Thread[threadsCount];


    for (int threadNumber = 1; threadNumber <= threads.Length; threadNumber++)
    {
        int capturedNumber = threadNumber;
        threads[threadNumber - 1] = new Thread(() =>
        {
            Consol.WriteLine($"Потік №{capturedNumber} [ID {Environment.CurrentManagedThreadId}] став у чергу.");
            semaphore.Wait();

            try
            {
                Consol.WriteLine($"Потік №{capturedNumber} [ID {Environment.CurrentManagedThreadId}] почав роботу.");

                var randomNumbers = new int[numbersPerThread];
                for (int i = 0; i < randomNumbers.Length; i++)
                {
                    randomNumbers[i] = Random.Shared.Next(0, 101);
                    Thread.Sleep(Random.Shared.Next(20, 61));
                }

                Consol.WriteLine($"Потік №{capturedNumber} [ID {Environment.CurrentManagedThreadId}]: " +
                      string.Join(", ", randomNumbers));
                Consol.WriteLine($"Потік №{capturedNumber} завершив роботу.");
            }
            finally
            {
                
                semaphore.Release();
            }
        })
        {
            Name = $"Number worker {capturedNumber}"
        };
    }

    foreach (Thread thread in threads) thread.Start();
    foreach (Thread thread in threads) thread.Join();

    Consol.WriteLine("Усі десять потоків завершили роботу.");
}
