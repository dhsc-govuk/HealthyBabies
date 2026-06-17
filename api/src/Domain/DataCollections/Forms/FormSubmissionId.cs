namespace Domain.DataCollections.Forms;

public record FormSubmissionId(Guid Value)
{
    public static FormSubmissionId Empty() => new(Guid.Empty);
    public static FormSubmissionId New() => new(Guid.NewGuid());
    public static FormSubmissionId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}