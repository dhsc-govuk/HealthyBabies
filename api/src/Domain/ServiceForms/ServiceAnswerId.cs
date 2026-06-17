namespace Domain.ServiceForms;

public record ServiceAnswerId(Guid Value)
{
    public static ServiceAnswerId Empty() => new(Guid.Empty);
    public static ServiceAnswerId New() => new(Guid.NewGuid());
    public static ServiceAnswerId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}