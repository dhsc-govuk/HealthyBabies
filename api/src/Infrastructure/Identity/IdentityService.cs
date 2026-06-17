using System.Security.Claims;
using Application.Common.Interfaces;
using Domain.Common;
using Domain.Locations;
using Domain.Organisations;
using Domain.Users;
using Domain.ValueObjects;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;

namespace Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly HttpContext _httpContext;

    public IdentityService(IHttpContextAccessor httpContextAccessor)
    {
        Guard.NotNull(httpContextAccessor, nameof(httpContextAccessor));
        Guard.NotNull(httpContextAccessor.HttpContext, nameof(httpContextAccessor.HttpContext));
        _httpContext = httpContextAccessor.HttpContext!;
    }

    public Option<UserId> GetUserId()
    {
        var userId = _httpContext.User.FindFirstValue("userId");
        return string.IsNullOrEmpty(userId)
            ? Option<UserId>.None
            : new UserId(new Guid(userId));
    }

    public Option<SubId> GetSubId()
    {
        var subId = _httpContext.User.FindFirstValue(ClaimConstants.ObjectId);
        return string.IsNullOrEmpty(subId)
            ? Option<SubId>.None
            : new SubId(new Guid(subId));
    }

    public Option<UserRole> GetRole()
    {
        var role = _httpContext.User.FindFirstValue(ClaimTypes.Role);
        return string.IsNullOrEmpty(role) ? Option<UserRole>.None : UserRole.From(role);
    }

    public Option<OrganisationId> GetOrganisationId()
    {
        var organisationId = _httpContext.User.FindFirstValue("organisationId");
        return string.IsNullOrEmpty(organisationId)
            ? Option<OrganisationId>.None
            : new OrganisationId(new Guid(organisationId));
    }

    public Option<LocationId> GetLocationId()
    {
        var locationId = _httpContext.User.FindFirstValue("locationId");
        return string.IsNullOrEmpty(locationId)
            ? Option<LocationId>.None
            : new LocationId(new Guid(locationId));
    }

    public Option<string> GetEmail()
    {
        var email = _httpContext.User.FindFirstValue(ClaimTypes.Email);
        return string.IsNullOrEmpty(email) ? Option<string>.None : email;
    }
}