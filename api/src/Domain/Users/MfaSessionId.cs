namespace Domain.Users;

public record MfaSessionId(Guid Value)
{
    public static MfaSessionId Empty() => new(Guid.Empty);
    public static MfaSessionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}