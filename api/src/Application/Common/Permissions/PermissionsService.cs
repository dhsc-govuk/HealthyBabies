using Application.Common.Interfaces;
using Domain.Users;
using Domain.ValueObjects;
using LanguageExt;

namespace Application.Common.Permissions;

public class PermissionsService(IIdentityService identityService)
{
    public Either<Exception, UserId> GetUserId()
    {
        var userId = identityService.GetUserId();
        return userId.Match<Either<Exception, UserId>>(
            id => id,
            new ArgumentException("User id is missing"));
    }

    public Either<Exception, Permission> GetOrganisationPermissions()
    {
        var userRole = identityService.GetRole();
        var userId = identityService.GetUserId();

        return userId.Match(
            uId => userRole.Match<Either<Exception, Permission>>(
                role => role.Equals(UserRole.Admin)
                    ? Permission.AdminPermission(role, uId)
                    : role.Equals(UserRole.OrganisationAdmin)
                        ? identityService.GetOrganisationId().Match<Either<Exception, Permission>>(
                            orgId => Permission.OrganisationPermission(role, orgId, uId),
                            () => new ArgumentException("Forbidden"))
                        : new ArgumentException("Forbidden"),
                () => new ArgumentException("User role is missing")),
            () => new ArgumentException("User id is missing"));
    }

    public Either<Exception, Permission> GetLocationPermissions()
    {
        var userRole = identityService.GetRole();
        var userId = identityService.GetUserId();

        return userId.Match(
            uId => userRole.Match<Either<Exception, Permission>>(
                role => role.Equals(UserRole.Admin)
                    ? Permission.AdminPermission(role, uId)
                    : role.Equals(UserRole.OrganisationAdmin)
                        ? identityService.GetOrganisationId().Match<Either<Exception, Permission>>(
                            orgId => Permission.OrganisationPermission(role, orgId, uId),
                            () => new ArgumentException("Organisation id is missing"))
                        : new ArgumentException("The current user is not authorized to access this resource."),
                () => new ArgumentException("User role is missing")),
            () => new ArgumentException("User id is missing"));
    }
}