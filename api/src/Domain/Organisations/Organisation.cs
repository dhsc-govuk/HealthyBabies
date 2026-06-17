using Domain.Common;

namespace Domain.Organisations;

public class Organisation : AuditableEntity<OrganisationId>
{
    public string Name { get; private set; }
    public string ONSCode { get; private set; }
    public bool IsActive { get; private set; }

    private Organisation(OrganisationId id, string name, string oNSCode, bool isActive)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(name, nameof(name));
        Guard.NotNullOrEmptyOrWhiteSpace(oNSCode, nameof(oNSCode));
        Id = id;
        Name = name;
        ONSCode = oNSCode;
        IsActive = isActive;
    }

    public static Organisation New(OrganisationId id, string name, string oNSCode, bool isActive)
    {
        return new Organisation(id, name, oNSCode, isActive);
    }

    public void UpdateDetails(string name, string oNSCode, bool isActive)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(name, nameof(name));
        Name = name;
        IsActive = isActive;
    }
}