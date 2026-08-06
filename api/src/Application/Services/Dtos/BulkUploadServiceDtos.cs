namespace Application.Services.Dtos;

public record BulkUploadRowValidationError(
    string QuestionCode,
    string Message);

public record BulkUploadServicesValidationResultDto(
    bool IsValid,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<BulkUploadServiceRowValidationDto> RowValidations,
    IReadOnlyList<BulkUploadServiceFieldMetadataDto> FieldMetadata,
    Guid StagingId);

public record BulkUploadServiceRowValidationDto(
    int RowNumber,
    string? ServiceName,
    bool IsValid,
    IReadOnlyList<BulkUploadServiceFieldErrorDto> Errors);

public record BulkUploadServiceFieldErrorDto(
    string FieldCode,
    string FieldLabel,
    string ErrorMessage);

public record BulkUploadServiceFieldMetadataDto(
    string FieldCode,
    string FieldType,
    bool IsRequired,
    IReadOnlyList<BulkUploadServiceFieldOptionDto> Options,
    string? ConditionalQuestionCode,
    string? ConditionalValue);

public record BulkUploadServiceFieldOptionDto(
    string Value,
    string Label);

public record BulkUploadServicesResultDto(
    bool Success,
    int TotalRows,
    int SuccessCount,
    int ErrorCount,
    IReadOnlyList<BulkUploadServiceRowResultDto> Results);

public record BulkUploadServiceRowResultDto(
    int RowNumber,
    string? ServiceName,
    bool IsSuccess,
    string? ErrorMessage = null,
    Guid? ServiceId = null,
    bool IsNew = false);