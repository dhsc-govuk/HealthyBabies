namespace Domain.DataCollections.Forms;

public record FormSubmissionHistoryId(Guid Value)
{
    public static FormSubmissionHistoryId Empty() => new(Guid.Empty);
    public static FormSubmissionHistoryId New() => new(Guid.NewGuid());
    public static FormSubmissionHistoryId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}