namespace Domain.DataCollections;

public record DataCollectionSubmissionVersionId(Guid Value)
{
    public static DataCollectionSubmissionVersionId Empty() => new(Guid.Empty);
    public static DataCollectionSubmissionVersionId New() => new(Guid.NewGuid());
    public static DataCollectionSubmissionVersionId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}