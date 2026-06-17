namespace Domain.DataCollections.Forms;

public record FormFieldId(Guid Value)
{
    public static FormFieldId Empty() => new(Guid.Empty);
    public static FormFieldId New() => new(Guid.NewGuid());
    public static FormFieldId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}