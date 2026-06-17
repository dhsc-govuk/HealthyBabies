namespace Domain.Services;

public record ServiceId(Guid Value)
{
    public static ServiceId Empty() => new(Guid.Empty);
    public static ServiceId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}