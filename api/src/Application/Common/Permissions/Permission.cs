using Domain.Common;
using Domain.Locations;
using Domain.Organisations;
using Domain.Users;
using Domain.ValueObjects;
using LanguageExt;

namespace Application.Common.Permissions;

public class Permission
{
    private Permission(
        UserRole userRole,
        Option<OrganisationId> organisationId,
        Option<LocationId> locationId,
        Option<UserId> userId)
    {
        Guard.NotNull(userRole, nameof(UserRole));
        UserRole = userRole;
        OrganisationId = organisationId;
        LocationId = locationId;
        UserId = userId;
    }

    public UserRole UserRole { get; }
    public Option<OrganisationId> OrganisationId { get; }
    public Option<LocationId> LocationId { get; }
    public Option<UserId> UserId { get; }

    public static Permission AdminPermission(UserRole userRole, UserId userId)
    {
        return new Permission(
            userRole,
            Option<OrganisationId>.None,
            Option<LocationId>.None,
            userId);
    }

    public static Permission OrganisationPermission(UserRole userRole, OrganisationId organisationId, UserId userId)
    {
        return new Permission(
            userRole,
            organisationId,
            Option<LocationId>.None,
            userId);
    }

    public static Permission LocationPermission(UserRole userRole, OrganisationId organisationId, LocationId locationId, UserId userId)
    {
        return new Permission(
            userRole,
            organisationId,
            locationId,
            userId);
    }
}