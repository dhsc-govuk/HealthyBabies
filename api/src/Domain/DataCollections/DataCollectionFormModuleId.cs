namespace Domain.DataCollections;

public record DataCollectionFormModuleId(Guid Value)
{
    public static DataCollectionFormModuleId Empty() => new(Guid.Empty);
    public static DataCollectionFormModuleId New() => new(Guid.NewGuid());
    public static DataCollectionFormModuleId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}