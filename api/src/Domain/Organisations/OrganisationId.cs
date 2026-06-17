namespace Domain.Organisations;

public record OrganisationId(Guid Value)
{
    public static OrganisationId Empty() => new(Guid.Empty);
    public static OrganisationId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}