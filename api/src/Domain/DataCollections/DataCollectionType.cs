using Domain.Common;

namespace Domain.DataCollections;

public static class DataCollectionTypeConstants
{
    public const string DeliveryPlan = "delivery-plan";
    public const string ManagementInformation = "management-information";
}

public class DataCollectionType : ValueObject
{
    public string Value { get; private set; }

    private DataCollectionType(string value)
    {
        Value = value;
    }

    public static DataCollectionType From(string value)
    {
        var type = new DataCollectionType(value);

        if (!SupportedTypes.Contains(type))
        {
            throw new UnsupportedDataCollectionTypeException(value);
        }

        return type;
    }

    public static DataCollectionType DeliveryPlan => new(DataCollectionTypeConstants.DeliveryPlan);
    public static DataCollectionType ManagementInformation => new(DataCollectionTypeConstants.ManagementInformation);

    public static implicit operator string(DataCollectionType type)
    {
        return type.ToString();
    }

    public static explicit operator DataCollectionType(string value)
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

    private static IEnumerable<DataCollectionType> SupportedTypes
    {
        get
        {
            yield return DeliveryPlan;
            yield return ManagementInformation;
        }
    }
}

public class UnsupportedDataCollectionTypeException(string value) : Exception($"DataCollectionType \"{value}\" is not supported.");