namespace Domain.Organisations;

public record OrganisationContactId(Guid Value)
{
    public static OrganisationContactId Empty() => new(Guid.Empty);
    public static OrganisationContactId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}