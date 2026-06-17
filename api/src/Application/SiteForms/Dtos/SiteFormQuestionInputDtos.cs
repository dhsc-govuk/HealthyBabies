namespace Application.SiteForms.Dtos;

public record CreateSiteFormQuestionDto(
    string Code,
    string Label,
    string? Hint,
    string? Placeholder,
    int QuestionType,
    bool IsRequired,
    string? HelpTextSummary,
    string? HelpText,
    string? ConditionalQuestionCode,
    string? ConditionalValue,
    IReadOnlyList<SiteFormQuestionOptionInputDto> Options);

public record UpdateSiteFormQuestionDto(
    string Label,
    string? Hint,
    string? Placeholder,
    int QuestionType,
    int DisplayOrder,
    bool IsRequired,
    bool IsActive,
    string? HelpTextSummary,
    string? HelpText,
    string? ConditionalQuestionCode,
    string? ConditionalValue,
    IReadOnlyList<SiteFormQuestionOptionInputDto> Options);

public record SiteFormQuestionOptionInputDto(
    string Value,
    string Label,
    int DisplayOrder);

public record SiteAnswerInputDto(
    string QuestionCode,
    string? Value);

public record ReorderSiteFormQuestionsDto(
    IReadOnlyList<SiteFormQuestionOrderDto> Questions);

public record SiteFormQuestionOrderDto(
    Guid Id,
    int DisplayOrder);