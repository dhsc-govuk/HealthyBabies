namespace Domain.Organisations;

public class OrganisationFinancialData : AuditableEntity<OrganisationFinancialDataId>
{
    public OrganisationId OrganisationId { get; private set; } = null!;
    public Organisation? Organisation { get; private set; }

    public FinancialDataModule Module { get; private set; } = null!;
    public string YearRange { get; private set; } = null!;
    public decimal Amount { get; private set; }

    private OrganisationFinancialData(
        OrganisationFinancialDataId id,
        OrganisationId organisationId,
        FinancialDataModule module,
        string yearRange,
        decimal amount)
    {
        Id = id;
        OrganisationId = organisationId;
        Module = module;
        YearRange = yearRange;
        Amount = amount;
        CreatedAt = DateTime.UtcNow;
    }

    public static OrganisationFinancialData New(
        OrganisationFinancialDataId id,
        OrganisationId organisationId,
        FinancialDataModule module,
        string yearRange,
        decimal amount)
        => new(id, organisationId, module, yearRange, amount);

    public void UpdateAmount(decimal amount)
    {
        Amount = amount;
        UpdatedAt = DateTime.UtcNow;
    }
}