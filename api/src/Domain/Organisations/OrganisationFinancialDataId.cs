namespace Domain.Organisations;

public record OrganisationFinancialDataId(Guid Value)
{
    public static OrganisationFinancialDataId Empty() => new(Guid.Empty);
    public static OrganisationFinancialDataId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}