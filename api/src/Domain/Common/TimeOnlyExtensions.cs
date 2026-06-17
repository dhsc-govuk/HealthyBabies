namespace Domain.Common;

public static class TimeOnlyExtensions
{
    public static TimeOnly Max(this TimeOnly a, TimeOnly b)
    {
        return a > b ? a : b;
    }

    public static TimeOnly Min(this TimeOnly a, TimeOnly b)
    {
        return a < b ? a : b;
    }
}