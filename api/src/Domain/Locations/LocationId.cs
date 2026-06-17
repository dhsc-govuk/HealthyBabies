namespace Domain.Locations;

public record LocationId(Guid Value)
{
    public static LocationId Empty() => new(Guid.Empty);
    public static LocationId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}