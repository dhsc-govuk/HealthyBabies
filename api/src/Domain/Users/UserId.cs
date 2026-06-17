namespace Domain.Users;

public record UserId(Guid Value)
{
    public static UserId Empty() => new(Guid.Empty);
    public static UserId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}