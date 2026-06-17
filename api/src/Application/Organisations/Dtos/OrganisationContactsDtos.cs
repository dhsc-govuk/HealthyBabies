namespace Application.Organisations.Dtos;

public record OrganisationContactDto(
    Guid? Id,
    Guid OrganisationId,
    string Name,
    string Email,
    string Role);

public record CreateOrganisationContactDto(
    Guid OrganisationId,
    string Name,
    string Email,
    string Role);

public record UpdateOrganisationContactDto(
    Guid Id,
    string Name,
    string Email,
    string Role);