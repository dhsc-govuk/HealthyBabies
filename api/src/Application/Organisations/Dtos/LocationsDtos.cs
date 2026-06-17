using Application.SiteForms.Dtos;

namespace Application.Organisations.Dtos;

public record LocationDto(
    Guid? Id,
    Guid? OrganisationId,
    string Name,
    string? PostCode,
    string? ReferenceNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? TownOrCity,
    string? County,
    bool IsActive,
    IReadOnlyList<SiteAnswerDto> Answers,
    OrganisationDto? Organisation = null);

public record CreateLocationInputDto(
    string? Name,
    string? PostCode,
    string? ReferenceNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? TownOrCity,
    string? County,
    IReadOnlyList<SiteAnswerInputDto> Answers);

public record UpdateLocationInputDto(
    Guid Id,
    string? Name,
    string? PostCode,
    string? ReferenceNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? TownOrCity,
    string? County,
    bool IsActive,
    IReadOnlyList<SiteAnswerInputDto> Answers);

public record LocationHomeDto(
    string LocationName,
    string OrganisationName,
    int Admins);

public record LocationAdminHomeDto(
    int Admins);

public record BulkUploadLocationsResult(
    int TotalRows,
    int SuccessCount,
    int ErrorCount,
    List<BulkUploadLocationResult> Results);

public record BulkUploadLocationResult(
    int RowNumber,
    string? SiteName,
    bool IsSuccess,
    string? ErrorMessage = null);

public record BulkUploadRowValidationError(
    string QuestionCode,
    string Message);