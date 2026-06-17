namespace Domain.DataCollections.Forms;

public record FormSectionId(Guid Value)
{
    public static FormSectionId New() => new(Guid.NewGuid());
    public static FormSectionId From(Guid value) => new(value);
}