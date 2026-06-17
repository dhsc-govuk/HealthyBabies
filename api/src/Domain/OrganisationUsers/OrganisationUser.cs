using Domain.Organisations;
using Domain.Users;

namespace Domain.OrganisationUsers;

public class OrganisationUser : IEntity<OrganisationUserId>
{
    public OrganisationUserId Id { get; private set; }
    public UserId UserId { get; private set; }
    public OrganisationId OrganisationId { get; private set; }
    public User? User { get; private set; }
    public Organisation? Organisation { get; private set; }

    private OrganisationUser(
        OrganisationUserId id,
        UserId userId,
        OrganisationId organisationId)
    {
        Id = id;
        UserId = userId;
        OrganisationId = organisationId;
    }

    public static OrganisationUser New(
        OrganisationUserId id,
        UserId userId,
        OrganisationId organisationId,
        User user)
    {
        return new OrganisationUser(id, userId, organisationId)
        {
            User = user
        };
    }

    public void Reactivate(OrganisationId organisationId, User user)
    {
        OrganisationId = organisationId;
        User = user;
    }
}