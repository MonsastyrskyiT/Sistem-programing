namespace GeometryLibrary;

public static class AreaCalculator
{
    public static double Square(double side)
    {
        ValidatePositive(side, nameof(side));
        return side * side;
    }

    public static double Rectangle(double width, double height)
    {
        ValidatePositive(width, nameof(width));
        ValidatePositive(height, nameof(height));
        return width * height;
    }

    public static double Triangle(double baseLength, double height)
    {
        ValidatePositive(baseLength, nameof(baseLength));
        ValidatePositive(height, nameof(height));
        return baseLength * height / 2;
    }

    private static void ValidatePositive(double value, string parameterName)
    {
        if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentOutOfRangeException(parameterName,
                "Значення має бути додатним скінченним числом.");
    }
}
