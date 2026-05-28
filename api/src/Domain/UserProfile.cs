using Domain.Locations;
using Domain.Organisations;
using Domain.OrganisationUsers;
using Domain.Users;
using Domain.ValueObjects;
using LanguageExt;

namespace Domain;

public class UserProfile
{
    public UserId? Id { get; private set; }
    public Name? Name { get; private set; }
    public string? Email { get; private set; }
    public bool IsActive { get; private set; }
    public UserRole? Role { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Option<OrganisationId> OrganisationId { get; private set; }
    public Option<LocationId> LocationId { get; private set; }
    public Option<string> OrganisationName { get; private set; }
    public string FullName => $"{Name?.FirstName} {Name?.LastName}";
    private static UserProfile New(
        UserId id,
        Name name,
        string email,
        bool isActive,
        UserRole role,
        DateTime? updatedAt,
        Option<OrganisationId> organisationId,
        Option<LocationId> locationId,
        Option<string> organisationName)
    {
        return new UserProfile
        {
            Id = id,
            Name = name,
            Email = email,
            IsActive = isActive,
            Role = role,
            UpdatedAt = updatedAt,
            OrganisationId = organisationId,
            LocationId = locationId,
            OrganisationName = organisationName
        };
    }

    public static UserProfile NewAdminProfile(User user)
    {
        return New(
            user.Id,
            user.Name,
            user.Email,
            user.IsActive,
            user.Role,
            user.UpdatedAt,
            Option<OrganisationId>.None,
            Option<LocationId>.None,
            Option<string>.None);
    }

    public static UserProfile NewOrganisationAdminProfile(OrganisationUser user)
    {
        return New(
            user.User!.Id,
            user.User.Name,
            user.User.Email,
            user.User.IsActive,
            user.User.Role,
            user.User.UpdatedAt,
            user.OrganisationId,
            Option<LocationId>.None,
            user.Organisation?.Name);
    }
}