namespace Application.Common.Dtos;

public record BulkUploadFieldErrorDto(
    string FieldCode,
    string FieldLabel,
    string ErrorMessage);

public record BulkUploadFieldOptionDto(
    string Value,
    string Label);

public record BulkUploadRowValidationDto(
    int RowNumber,
    string? EntityName,
    bool IsValid,
    IReadOnlyList<BulkUploadFieldErrorDto> Errors);