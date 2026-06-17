namespace Application.ServiceForms.Dtos;

public record CreateServiceFormQuestionDto(
    string Code,
    string Label,
    string? Hint,
    string? Placeholder,
    int QuestionType,
    int Step,
    bool IsRequired,
    bool IsPredefined,
    string? HelpText,
    string? ConditionalQuestionCode,
    string? ConditionalValue,
    IReadOnlyList<ServiceFormQuestionOptionInputDto> Options);

public record UpdateServiceFormQuestionDto(
    string Label,
    string? Hint,
    string? Placeholder,
    int QuestionType,
    int Step,
    int DisplayOrder,
    bool IsRequired,
    bool IsPredefined,
    bool IsActive,
    string? HelpText,
    string? ConditionalQuestionCode,
    string? ConditionalValue,
    IReadOnlyList<ServiceFormQuestionOptionInputDto> Options);

public record ServiceFormQuestionOptionInputDto(
    string Value,
    string Label,
    int DisplayOrder);

public record ReorderQuestionsDto(
    int Step,
    IReadOnlyList<QuestionOrderDto> Questions);

public record QuestionOrderDto(
    Guid Id,
    int DisplayOrder);