using Domain.Organisations;
using Domain.Users;
using Domain.ValueObjects;

namespace Tests.Data;

public static class OrganisationAdminUsersData
{
    public static readonly OrganisationId OrganisationId = OrganisationId.New();

    public static readonly User MainOrganisationAdminUser = User.New(
        id: UserId.New(),
        name: new Name("Organisation", "Admin"),
        email: "organisation.admin@email.com",
        subId: new SubId(Guid.NewGuid()),
        isActive: true,
        role: UserRole.OrganisationAdmin);
}