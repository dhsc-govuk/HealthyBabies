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

    public static readonly Dictionary<string, string> Entities = new()
    {
        { ContactRole, "Contact roles for organisation contacts" },
        { ServiceCategory, "Categories of services" },
        { LocationType, "Types of locations" },
        { Status, "Status values for various entities" },
        { TypeOfSites, "Types of sites" },
        { SiteStatus, "Status values for sites" },
        { WiderServiceCategory, "Wider service categories for Family Hubs" }
    };

    public static bool IsValidEntity(string entity) => Entities.ContainsKey(entity);

    public static IReadOnlyList<string> GetEntityNames() => Entities.Keys.ToList();
}