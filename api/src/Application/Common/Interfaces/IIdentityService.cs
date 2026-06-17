using Domain.Locations;
using Domain.Organisations;
using Domain.Users;
using Domain.ValueObjects;
using LanguageExt;

namespace Application.Common.Interfaces;

public interface IIdentityService
{
    Option<UserId> GetUserId();
    Option<SubId> GetSubId();
    Option<UserRole> GetRole();
    Option<OrganisationId> GetOrganisationId();
    Option<LocationId> GetLocationId();
    Option<string> GetEmail();
}