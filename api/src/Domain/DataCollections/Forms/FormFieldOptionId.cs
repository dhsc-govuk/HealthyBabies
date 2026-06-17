namespace Domain.DataCollections.Forms;

public record FormFieldOptionId(Guid Value)
{
    public static FormFieldOptionId Empty() => new(Guid.Empty);
    public static FormFieldOptionId New() => new(Guid.NewGuid());
    public static FormFieldOptionId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}