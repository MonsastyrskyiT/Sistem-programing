namespace FractionLibrary;

public sealed class Fraction : IEquatable<Fraction>
{
    public long Numerator { get; }
    public long Denominator { get; }

    public Fraction(long numerator, long denominator)
    {
        if (denominator == 0)
            throw new DivideByZeroException("Знаменник дробу не може дорівнювати нулю.");

        if (denominator < 0)
        {
            numerator = checked(-numerator);
            denominator = checked(-denominator);
        }

        long divisor = GreatestCommonDivisor(Math.Abs(numerator), denominator);
        Numerator = numerator / divisor;
        Denominator = denominator / divisor;
    }

    public Fraction Add(Fraction other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new Fraction(
            checked(Numerator * other.Denominator + other.Numerator * Denominator),
            checked(Denominator * other.Denominator));
    }

    public Fraction Subtract(Fraction other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new Fraction(
            checked(Numerator * other.Denominator - other.Numerator * Denominator),
            checked(Denominator * other.Denominator));
    }

    public Fraction Multiply(Fraction other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new Fraction(
            checked(Numerator * other.Numerator),
            checked(Denominator * other.Denominator));
    }

    public Fraction Divide(Fraction other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other.Numerator == 0)
            throw new DivideByZeroException("Не можна ділити на нульовий дріб.");

        return new Fraction(
            checked(Numerator * other.Denominator),
            checked(Denominator * other.Numerator));
    }

    public Fraction Reciprocal()
    {
        if (Numerator == 0)
            throw new DivideByZeroException("Нульовий дріб не має оберненого дробу.");
        return new Fraction(Denominator, Numerator);
    }

    public double ToDouble() => (double)Numerator / Denominator;

    public override string ToString() => $"{Numerator}/{Denominator}";

    public bool Equals(Fraction? other) =>
        other is not null && Numerator == other.Numerator && Denominator == other.Denominator;

    public override bool Equals(object? obj) => Equals(obj as Fraction);

    public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);

    public static Fraction operator +(Fraction left, Fraction right) => left.Add(right);
    public static Fraction operator -(Fraction left, Fraction right) => left.Subtract(right);
    public static Fraction operator *(Fraction left, Fraction right) => left.Multiply(right);
    public static Fraction operator /(Fraction left, Fraction right) => left.Divide(right);

    private static long GreatestCommonDivisor(long first, long second)
    {
        while (second != 0)
        {
            long remainder = first % second;
            first = second;
            second = remainder;
        }

        return first == 0 ? 1 : first;
    }
}
