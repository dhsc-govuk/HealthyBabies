using Domain.Common;

namespace Domain.DataCollections;

public static class DataCollectionStatusConstants
{
    public const string Draft = "draft";
    public const string Planned = "planned";
    public const string Open = "open";
    public const string Closed = "closed";
}

/// <summary>
/// Status of a data collection.
/// </summary>
public class DataCollectionStatus : ValueObject
{
    public string Value { get; private set; }

    private DataCollectionStatus(string value)
    {
        Value = value;
    }

    public static DataCollectionStatus From(string value)
    {
        var status = new DataCollectionStatus(value);

        if (!SupportedStatuses.Contains(status))
        {
            throw new UnsupportedDataCollectionStatusException(value);
        }

        return status;
    }

    public static DataCollectionStatus Draft => new(DataCollectionStatusConstants.Draft);
    public static DataCollectionStatus Planned => new(DataCollectionStatusConstants.Planned);
    public static DataCollectionStatus Open => new(DataCollectionStatusConstants.Open);
    public static DataCollectionStatus Closed => new(DataCollectionStatusConstants.Closed);

    public static implicit operator string(DataCollectionStatus status)
    {
        return status.ToString();
    }

    public static explicit operator DataCollectionStatus(string value)
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

    private static IEnumerable<DataCollectionStatus> SupportedStatuses
    {
        get
        {
            yield return Draft;
            yield return Planned;
            yield return Open;
            yield return Closed;
        }
    }
}

public class UnsupportedDataCollectionStatusException : Exception
{
    public UnsupportedDataCollectionStatusException(string value) : base($"DataCollectionStatus \"{value}\" is not supported.")
    {
    }
}