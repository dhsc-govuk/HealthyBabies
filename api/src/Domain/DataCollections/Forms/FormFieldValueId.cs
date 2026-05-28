namespace Domain.DataCollections.Forms;

public record FormFieldValueId(Guid Value)
{
    public static FormFieldValueId Empty() => new(Guid.Empty);
    public static FormFieldValueId New() => new(Guid.NewGuid());
    public static FormFieldValueId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}