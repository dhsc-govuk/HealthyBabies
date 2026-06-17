namespace Domain.OrganisationUsers;

public record OrganisationUserId(Guid Value)
{
    public static OrganisationUserId Empty() => new(Guid.Empty);
    public static OrganisationUserId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}