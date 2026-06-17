using Domain.DataCollections.Forms;

namespace Application.DataCollections.Dtos;

public record FormFieldDto(
    Guid Id,
    Guid FormModuleId,
    Guid? FormSectionId,
    string FieldKey,
    string Label,
    string FieldType,
    int DisplayOrder,
    bool IsRequired,
    string? Placeholder,
    string? HelpText,
    string? DefaultValue,
    string? ValidationRules,
    string? ConditionalRules,
    string? Configuration,
    bool IsActive,
    IReadOnlyList<FormFieldOptionDto> Options)
{
    public static FormFieldDto FromDomainModel(FormField field) =>
        new(
            field.Id.Value,
            field.FormModuleId.Value,
            field.FormSectionId?.Value,
            field.FieldKey,
            field.Label,
            field.FieldType.Value,
            field.DisplayOrder,
            field.IsRequired,
            field.Placeholder,
            field.HelpText,
            field.DefaultValue,
            field.ValidationRules,
            field.ConditionalRules,
            field.Configuration,
            field.IsActive,
            field.Options.Select(FormFieldOptionDto.FromDomainModel).ToList());
}

public record FormFieldOptionDto(
    Guid Id,
    string Value,
    string Label,
    int DisplayOrder,
    bool IsDefault)
{
    public static FormFieldOptionDto FromDomainModel(FormFieldOption option) =>
        new(
            option.Id.Value,
            option.Value,
            option.Label,
            option.DisplayOrder,
            option.IsDefault);
}