namespace Domain.DataCollections;

public record DataCollectionId(Guid Value)
{
    public static DataCollectionId Empty() => new(Guid.Empty);
    public static DataCollectionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}