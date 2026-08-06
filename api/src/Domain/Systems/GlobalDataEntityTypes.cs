namespace Domain.Systems;

public static class GlobalDataEntityTypes
{
    public const string ContactRole = "CONTACT_ROLES";
    public const string ServiceCategory = "SERVICE_CATEGORIES";
    public const string LocationType = "LOCATION_TYPES";
    public const string Status = "STATUS";

    public const string TypeOfSites = "SITES_TYPES";
    public const string SiteStatus = "SITE_STATUS";
    public const string WiderServiceCategory = "WIDER_SERVICE_CATEGORIES";

    public const string FinancialDataAllocationModule = "FINANCIAL_DATA_ALLOCATION_MODULE";
    public const string FinancialDataAllocationYear = "FINANCIAL_DATA_ALLOCATION_YEAR";
    public const string FinancialDataRolloverModule = "FINANCIAL_DATA_ROLLOVER_MODULE";
    public const string FinancialDataRolloverYear = "FINANCIAL_DATA_ROLLOVER_YEAR";

    public static readonly Dictionary<string, string> Entities = new()
    {
        { ContactRole, "Contact roles for organisation contacts" },
        { ServiceCategory, "Categories of services" },
        { LocationType, "Types of locations" },
        { Status, "Status values for various entities" },
        { TypeOfSites, "Types of sites" },
        { SiteStatus, "Status values for sites" },
        { WiderServiceCategory, "Wider service categories for Family Hubs" },
        { FinancialDataAllocationModule, "Allocation modules for financial data (e.g. delivery-grant, parenting)" },
        { FinancialDataAllocationYear, "Year ranges for allocation modules (e.g. 2026-27, 2027-28)" },
        { FinancialDataRolloverModule, "Rollover modules for financial data (e.g. rollover-spend)" },
        { FinancialDataRolloverYear, "Year ranges for rollover modules (e.g. 2025-26)" }
    };

    public static bool IsValidEntity(string entity) => Entities.ContainsKey(entity);

    public static IReadOnlyList<string> GetEntityNames() => Entities.Keys.ToList();
}