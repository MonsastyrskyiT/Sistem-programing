namespace ParalerSistem2;

internal sealed record UserRecord(string FullName, string Username)
{
    public override string ToString() => $"{FullName} - {Username}";
}
