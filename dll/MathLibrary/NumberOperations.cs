using System.Numerics;

namespace MathLibrary;

public static class NumberOperations
{
    public static BigInteger Factorial(int number)
    {
        if (number < 0)
            throw new ArgumentOutOfRangeException(nameof(number),
                "Факторіал визначений тільки для невід'ємних чисел.");

        BigInteger result = BigInteger.One;
        for (int multiplier = 2; multiplier <= number; multiplier++)
            result *= multiplier;

        return result;
    }

    public static bool IsPrime(long number)
    {
        if (number < 2) return false;
        if (number == 2) return true;
        if (number % 2 == 0) return false;
        for (long divisor = 3; divisor <= number / divisor; divisor += 2)
        {
            if (number % divisor == 0) return false;
        }

        return true;
    }

    public static bool IsEven(long number) => number % 2 == 0;

    public static bool IsOdd(long number) => !IsEven(number);
}
