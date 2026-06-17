namespace Domain.DataCollections;

public record DataCollectionSubmissionId(Guid Value)
{
    public static DataCollectionSubmissionId Empty() => new(Guid.Empty);
    public static DataCollectionSubmissionId New() => new(Guid.NewGuid());
    public static DataCollectionSubmissionId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}