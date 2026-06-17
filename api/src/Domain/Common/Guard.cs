namespace Domain.Common;

public static class Guard
{
    public static void NotEmpty<T>([ValidatedNotNull] IReadOnlyCollection<T> value, string name, string? message = null) where T : class
    {
        if (value.Count == 0)
        {
            throw new ArgumentException(name, message ?? "Collection cannot be empty");
        }
    }

    public static void NotNull<T>([ValidatedNotNull] T value, string name, string? message = null) where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(name, message ?? "Null argument received");
        }
    }

    public static void NotNullOrEmpty([ValidatedNotNull] string value, string name, string? message = null)
    {
        NotNull(value, name, message);
        if (value == string.Empty)
        {
            throw new ArgumentException(name, message ?? "Empty argument received");
        }
    }

    public static void NotNullOrEmptyOrWhiteSpace([ValidatedNotNull] string value, string name, string? message = null)
    {
        NotNullOrEmpty(value, name, message);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(name, message ?? "Whitespace received");
        }
    }

    public static void GreaterThan<T>(T numericValue, T expected, string name, string? message = null)
        where T : struct, IComparable<T>
    {
        if (numericValue.CompareTo(expected) <= 0)
        {
            throw new ArgumentOutOfRangeException(name, message ?? $"Value must be greater than {expected.ToString()}");
        }
    }

    public static void GreaterThanOrEqual<T>(T numericValue, T expected, string name, string? message = null)
        where T : struct, IComparable<T>
    {
        if (numericValue.CompareTo(expected) < 0)
        {
            throw new ArgumentOutOfRangeException(name, message ?? $"Value must be greater than or equal to {expected.ToString()}");
        }
    }

    public static void LessThan<T>(T numericValue, T expected, string name, string? message = null)
        where T : struct, IComparable<T>
    {
        if (numericValue.CompareTo(expected) >= 0)
        {
            throw new ArgumentOutOfRangeException(name, message ?? $"Value must be less than {expected.ToString()}");
        }
    }

    public static void LessThanOrEqual<T>(T numericValue, T expected, string name, string? message = null)
        where T : struct, IComparable<T>
    {
        if (numericValue.CompareTo(expected) > 0)
        {
            throw new ArgumentOutOfRangeException(name, message ?? $"Value must be less than or equal to {expected.ToString()}");
        }
    }

    public static void LessThanOrEqual(string stringValue, int maxLength, string name, string? message = null)
    {
        if (stringValue.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(name, message ?? $"Value length must be less than or equal to {maxLength.ToString()}");
        }
    }

    public static void IsTrue(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new ArgumentException(message ?? $"Condition must be true");
        }
    }

    public static void IsFalse(bool condition, string? message = null)
    {
        if (condition)
        {
            throw new ArgumentException(message ?? $"Condition must be false");
        }
    }

    public static void NotNullOrEmpty<T>([ValidatedNotNull] IEnumerable<T> collection, string name, string? message = null)
    {
        if (collection is null || !collection.Any())
        {
            throw new ArgumentException(name, message ?? "Collection can not be null or empty");
        }
    }

    public static void NotContainNulls<T>(IEnumerable<T> collection, string name, string? message = null) where T : class
    {
        if (collection.Any(i => i is null))
        {
            throw new ArgumentException(name, message ?? "Collection can not contain nulls");
        }
    }

    public static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new ArgumentException(message);
        }
    }
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ValidatedNotNullAttribute : Attribute
{
}