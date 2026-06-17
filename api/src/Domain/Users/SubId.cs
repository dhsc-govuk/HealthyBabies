namespace Domain.Users;

public record SubId(Guid Value)
{
    public static SubId Empty() => new(Guid.Empty);
    public static SubId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}