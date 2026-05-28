using Domain.Common;

namespace Domain.Organisations;

public class OrganisationContact : SoftDeletableEntity<OrganisationContactId>
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string Role { get; private set; }
    public string? RoleTitle { get; private set; }

    public OrganisationId OrganisationId { get; private set; }
    public Organisation? Organisation { get; private set; }

    private OrganisationContact(OrganisationContactId id, OrganisationId organisationId, string name, string email, string role, string? roleTitle = null)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(name, nameof(name));
        Guard.NotNullOrEmptyOrWhiteSpace(email, nameof(email));
        Guard.NotNullOrEmptyOrWhiteSpace(role, nameof(role));

        Id = id;
        OrganisationId = organisationId;
        Name = name;
        Email = email;
        Role = role;
        RoleTitle = roleTitle;
    }

    public static OrganisationContact New(
        OrganisationContactId id,
        OrganisationId organisationId,
        string name,
        string email,
        string role,
        string? roleTitle = null)
        => new(id, organisationId, name, email, role, roleTitle);

    public void UpdateDetails(string name, string email, string role, string? roleTitle = null)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(name, nameof(name));
        Guard.NotNullOrEmptyOrWhiteSpace(email, nameof(email));
        Name = name;
        Email = email;
        Role = role;
        RoleTitle = roleTitle;
    }
}