namespace Sinhron;

/// <summary>
/// Банківський рахунок із потокобезпечними операціями.
/// </summary>
internal sealed class BankAccount
{
    private readonly object _balanceLock = new();
    private decimal _balance;

    public BankAccount(decimal initialBalance)
    {
        if (initialBalance < 0)
            throw new ArgumentOutOfRangeException(nameof(initialBalance));

        _balance = initialBalance;
    }

    public decimal Balance
    {
        get
        {
            lock (_balanceLock)
            {
                return _balance;
            }
        }
    }

    public TransactionResult Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        lock (_balanceLock)
        {
            _balance += amount;
            return new TransactionResult(amount, _balance);
        }
    }

    public TransactionResult Withdraw(decimal requestedAmount)
    {
        if (requestedAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedAmount));

        lock (_balanceLock)
        {
            // Сума зняття ніколи не перевищує поточний баланс.
            decimal withdrawnAmount = Math.Min(requestedAmount, _balance);
            _balance -= withdrawnAmount;
            return new TransactionResult(withdrawnAmount, _balance);
        }
    }
}

internal readonly record struct TransactionResult(decimal Amount, decimal NewBalance);
