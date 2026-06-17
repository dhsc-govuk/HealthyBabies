using Domain.OrganisationUsers;
using Domain.Users;

namespace Application.Users.Dtos;

public record AdminTotalsResponse(
    int Admins,
    int Organisations);

public record UserDto(
    Guid? Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    string? Role,
    string? MfaStatus = null)
{
    public static UserDto FromDomainModel(User input, string? mfaStatus = null)
        => new(
            Id: input.Id.Value,
            FirstName: input.Name.FirstName,
            LastName: input.Name.LastName,
            Email: input.Email,
            IsActive: input.IsActive,
            Role: input.Role.ToString(),
            MfaStatus: mfaStatus);

    public static UserDto FromDomainModel(OrganisationUser input, string? mfaStatus = null)
        => new(
            Id: input.Id.Value,
            FirstName: input.User!.Name.FirstName,
            LastName: input.User.Name.LastName,
            Email: input.User.Email,
            IsActive: input.User.IsActive,
            Role: input.User.Role.ToString(),
            MfaStatus: mfaStatus);
}

public record AdminUserDto(
    string? Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    string? MfaStatus = null);

public record OrganisationUserDto(
    string? Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    string Role,
    string? Organisation = null,
    string? OrganisationId = null,
    string? MfaStatus = null);

public record Profile(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    string Role,
    Guid? OrganisationId,
    Guid? LocationId,
    string? OrganisationName);

public record OrganisationUserQueryDto(Guid? OrganisationId, int PageSize, int PageNumber);

public record ActivateOrDeactivateUserDto(
    Guid Id,
    bool IsActive);

public record OrganisationUserSimpleDto(
    string FirstName,
    string LastName);