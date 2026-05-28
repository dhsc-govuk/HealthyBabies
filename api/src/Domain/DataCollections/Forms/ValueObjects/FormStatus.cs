using Domain.Common;

namespace Domain.DataCollections.Forms.ValueObjects;

public static class FormStatusConstants
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Archived = "archived";
    public const string Deprecated = "deprecated";
}

/// <summary>
/// Status of a form version.
/// </summary>
public class FormStatus : ValueObject
{
    public string Value { get; private set; }

    private FormStatus(string value)
    {
        Value = value;
    }

    public static FormStatus From(string value)
    {
        var status = new FormStatus(value);

        if (!SupportedStatuses.Contains(status))
        {
            throw new UnsupportedFormStatusException(value);
        }

        return status;
    }

    public static FormStatus Draft => new(FormStatusConstants.Draft);
    public static FormStatus Published => new(FormStatusConstants.Published);
    public static FormStatus Archived => new(FormStatusConstants.Archived);
    public static FormStatus Deprecated => new(FormStatusConstants.Deprecated);

    public static implicit operator string(FormStatus status)
    {
        return status.ToString();
    }

    public static explicit operator FormStatus(string value)
    {
        return From(value);
    }

    public override string ToString()
    {
        return Value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    private static IEnumerable<FormStatus> SupportedStatuses
    {
        get
        {
            yield return Draft;
            yield return Published;
            yield return Archived;
            yield return Deprecated;
        }
    }
}

public class UnsupportedFormStatusException(string value) : Exception($"FormStatus \"{value}\" is not supported.");