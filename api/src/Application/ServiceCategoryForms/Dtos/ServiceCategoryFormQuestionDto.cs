using Domain.ServiceCategoryForms;

namespace Application.ServiceCategoryForms.Dtos;

public record ServiceCategoryFormQuestionDto(
    Guid Id,
    string Code,
    string Label,
    string? Hint,
    string? Placeholder,
    int QuestionType,
    int Step,
    int DisplayOrder,
    bool IsRequired,
    bool IsPredefined,
    string? HelpTextSummary,
    string? HelpText,
    string? ConditionalQuestionCode,
    string? ConditionalValue,
    bool IsActive,
    IReadOnlyList<ServiceCategoryFormQuestionOptionDto> Options)
{
    public static ServiceCategoryFormQuestionDto FromDomainModel(ServiceCategoryFormQuestion question) =>
        new(
            question.Id.Value,
            question.Code,
            question.Label,
            question.Hint,
            question.Placeholder,
            (int)question.QuestionType,
            question.Step,
            question.DisplayOrder,
            question.IsRequired,
            question.IsPredefined,
            question.HelpTextSummary,
            question.HelpText,
            question.ConditionalQuestionCode,
            question.ConditionalValue,
            question.IsActive,
            question.Options.Select(ServiceCategoryFormQuestionOptionDto.FromDomainModel).ToList());
}

public record ServiceCategoryFormQuestionOptionDto(
    Guid Id,
    string Value,
    string Label,
    int DisplayOrder)
{
    public static ServiceCategoryFormQuestionOptionDto FromDomainModel(ServiceCategoryFormQuestionOption option) =>
        new(
            option.Id.Value,
            option.Value,
            option.Label,
            option.DisplayOrder);
}