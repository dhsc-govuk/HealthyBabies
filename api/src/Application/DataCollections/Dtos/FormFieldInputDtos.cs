namespace Application.DataCollections.Dtos;

public record CreateFormFieldDto(
    Guid FormModuleId,
    Guid? FormSectionId,
    string FieldKey,
    string Label,
    string FieldType,
    bool IsRequired,
    string? Placeholder,
    string? HelpText,
    string? DefaultValue,
    string? ValidationRules,
    string? ConditionalRules,
    string? Configuration,
    IReadOnlyList<FormFieldOptionInputDto> Options);

public record UpdateFormFieldDto(
    Guid? FormSectionId,
    string Label,
    string FieldType,
    int DisplayOrder,
    bool IsRequired,
    bool IsActive,
    string? Placeholder,
    string? HelpText,
    string? DefaultValue,
    string? ValidationRules,
    string? ConditionalRules,
    string? Configuration,
    IReadOnlyList<FormFieldOptionInputDto> Options);

public record FormFieldOptionInputDto(
    string Value,
    string Label,
    int DisplayOrder,
    bool IsDefault = false);

public record ReorderFormFieldsDto(
    Guid FormModuleId,
    IReadOnlyList<FormFieldOrderDto> Fields);

public record FormFieldOrderDto(
    Guid Id,
    int DisplayOrder);