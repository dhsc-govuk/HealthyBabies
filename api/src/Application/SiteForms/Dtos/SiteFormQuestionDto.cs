using Domain.SiteForms;

namespace Application.SiteForms.Dtos;

public record SiteFormQuestionDto(
    Guid Id,
    string Code,
    string Label,
    string? Hint,
    string? Placeholder,
    int QuestionType,
    int DisplayOrder,
    bool IsRequired,
    bool IsPredefined,
    string? HelpTextSummary,
    string? HelpText,
    string? ConditionalQuestionCode,
    string? ConditionalValue,
    string? ConditionalRules,
    bool IsActive,
    string? DateValidationRule,
    IReadOnlyList<SiteFormQuestionOptionDto> Options)
{
    public static SiteFormQuestionDto FromDomainModel(SiteFormQuestion question) =>
        new(
            question.Id.Value,
            question.Code,
            question.Label,
            question.Hint,
            question.Placeholder,
            (int)question.QuestionType,
            question.DisplayOrder,
            question.IsRequired,
            question.IsPredefined,
            question.HelpTextSummary,
            question.HelpText,
            question.ConditionalQuestionCode,
            question.ConditionalValue,
            question.ConditionalRules,
            question.IsActive,
            question.DateValidationRule,
            question.Options.Select(SiteFormQuestionOptionDto.FromDomainModel).ToList());
}

public record SiteFormQuestionOptionDto(
    Guid Id,
    string Value,
    string Label,
    int DisplayOrder)
{
    public static SiteFormQuestionOptionDto FromDomainModel(SiteFormQuestionOption option) =>
        new(
            option.Id.Value,
            option.Value,
            option.Label,
            option.DisplayOrder);
}