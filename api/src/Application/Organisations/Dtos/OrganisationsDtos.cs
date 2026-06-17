namespace Application.Organisations.Dtos;

public record OrganisationDto(

    Guid? Id,

    string Name,

    string? ONSCode,

    bool IsActive);

public record CreateContactDto(

    string FullName,

    string Role,

    string? RoleTitle,

    string Email);

public record CreateOrganisationDto(

    string? Id,

    string Name,

    string? ONSCode,

    bool IsActive,

    List<CreateContactDto>? Contacts = null);

public record OrganisationContactDetailDto(

    string FullName,

    string Role,

    string? RoleTitle,

    string Email);

public record OrganisationHomeDto(

    string Name,

    string? ONSCode,

    bool IsActive,

    int Admins,

    int Locations,

    List<OrganisationContactDetailDto> ContactDetails,

    string? CreatedBy,

    DateTime? CreatedAt,

    string? LastChangedBy,

    DateTime? LastChangedAt);

public record OrganisationAdminHomeDto(

    int Admins,

    int Locations,

    int Contacts);