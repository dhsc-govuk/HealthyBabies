namespace Domain.Users;

public record UserMfaId(Guid Value)
{
    public static UserMfaId Empty() => new(Guid.Empty);
    public static UserMfaId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}