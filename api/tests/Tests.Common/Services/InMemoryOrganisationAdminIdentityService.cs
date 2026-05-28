using Application.Common.Interfaces;
using Domain.Locations;
using Domain.Organisations;
using Domain.Users;
using Domain.ValueObjects;
using LanguageExt;
using Tests.Data;

namespace Tests.Common.Services;

public class InMemoryOrganisationAdminIdentityService : IIdentityService
{
    public Option<UserId> GetUserId()
    {
        return OrganisationAdminUsersData.MainOrganisationAdminUser.Id;
    }

    public Option<SubId> GetSubId()
    {
        return OrganisationAdminUsersData.MainOrganisationAdminUser.SubId;
    }

    public Option<UserRole> GetRole()
    {
        return UserRole.OrganisationAdmin;
    }

    public Option<OrganisationId> GetOrganisationId()
    {
        return OrganisationAdminUsersData.OrganisationId;
    }

    public Option<LocationId> GetLocationId()
    {
        return Option<LocationId>.None;
    }

    public Option<string> GetEmail()
    {
        return "localauthority@test.com";
    }
}