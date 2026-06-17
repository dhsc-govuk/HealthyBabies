using Domain.Organisations;
using Domain.OrganisationUsers;
using Domain.Users;
using Domain.ValueObjects;

namespace Tests.Data;

public static class OrganisationUsersData
{
    public static OrganisationUser MainOrganisationUser(OrganisationId organisationId)
    {
        var user = User.New(
            id: UserId.New(),
            name: new Name("main organisation", "admin"),
            email: "main.organisation.admin@org.com",
            new SubId(Guid.NewGuid()),
            isActive: true,
            role: UserRole.OrganisationAdmin);

        return OrganisationUser.New(
            id: OrganisationUserId.New(),
            userId: user.Id,
            organisationId: organisationId,
            user: user);
    }

    public static OrganisationUser SecondOrganisationUser(OrganisationId organisationId)
    {
        var user = User.New(
            id: UserId.New(),
            name: new Name("second organisation", "admin"),
            email: "second.organisation.admin@org.com",
            new SubId(Guid.NewGuid()),
            isActive: true,
            role: UserRole.OrganisationAdmin);

        return OrganisationUser.New(
            id: OrganisationUserId.New(),
            userId: user.Id,
            organisationId: organisationId,
            user: user);
    }
}