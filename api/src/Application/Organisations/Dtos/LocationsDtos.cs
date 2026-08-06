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

public record BulkUploadRowValidationError(
    string QuestionCode,
    string Message);

public record BulkUploadLocationsValidationResultDto(
    bool IsValid,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<BulkUploadRowValidationDto> RowValidations,
    IReadOnlyList<BulkUploadFieldMetadataDto> FieldMetadata,
    Guid StagingId);

public record BulkUploadRowValidationDto(
    int RowNumber,
    string? SiteName,
    bool IsValid,
    IReadOnlyList<BulkUploadFieldErrorDto> Errors);

public record BulkUploadFieldErrorDto(
    string FieldCode,
    string FieldLabel,
    string ErrorMessage);

public record BulkUploadFieldMetadataDto(
    string FieldCode,
    string FieldType,
    bool IsRequired,
    IReadOnlyList<BulkUploadFieldOptionDto> Options,
    string? ConditionalQuestionCode,
    string? ConditionalValue);

public record BulkUploadFieldOptionDto(
    string Value,
    string Label);