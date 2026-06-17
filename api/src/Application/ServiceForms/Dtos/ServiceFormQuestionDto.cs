using Domain.ServiceForms;

namespace Application.ServiceForms.Dtos;

public record ServiceFormQuestionDto(
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
    IReadOnlyList<ServiceFormQuestionOptionDto> Options)
{
    public static ServiceFormQuestionDto FromDomainModel(ServiceFormQuestion question) =>
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
            question.Options.Select(ServiceFormQuestionOptionDto.FromDomainModel).ToList());
}

public record ServiceFormQuestionOptionDto(
    Guid Id,
    string Value,
    string Label,
    int DisplayOrder)
{
    public static ServiceFormQuestionOptionDto FromDomainModel(ServiceFormQuestionOption option) =>
        new(
            option.Id.Value,
            option.Value,
            option.Label,
            option.DisplayOrder);
}