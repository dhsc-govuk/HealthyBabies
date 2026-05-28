using Application.Common.Interfaces;
using Domain.Locations;
using Domain.Organisations;
using Domain.Users;
using Domain.ValueObjects;
using LanguageExt;
using Tests.Data;

namespace Tests.Common.Services;

public class InMemoryIdentityService : IIdentityService
{
    public Option<UserId> GetUserId()
    {
        return AdminsData.MainAdmin.Id;
    }

    public Option<SubId> GetSubId()
    {
        return AdminsData.MainAdmin.SubId;
    }

    public Option<UserRole> GetRole()
    {
        return UserRole.Admin;
    }

    public Option<OrganisationId> GetOrganisationId()
    {
        return Option<OrganisationId>.None;
    }

    public Option<LocationId> GetLocationId()
    {
        return Option<LocationId>.None;
    }

    public Option<string> GetEmail()
    {
        return "admin@test.com";
    }
}