namespace Domain.Systems;

public record GlobalDataId(Guid Value)
{
    public static GlobalDataId Empty() => new(Guid.Empty);
    public static GlobalDataId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}