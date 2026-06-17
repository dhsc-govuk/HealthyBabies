using Domain.ServiceForms;

namespace Application.ServiceForms.Dtos;

public record ServiceAnswerDto(
    Guid Id,
    string QuestionCode,
    string QuestionLabel,
    string? QuestionHint,
    int QuestionType,
    int Step,
    int DisplayOrder,
    string? Value,
    string? DisplayValue,
    string? OptionsSnapshot)
{
    public static ServiceAnswerDto FromDomainModel(ServiceAnswer answer) =>
        new(
            answer.Id.Value,
            answer.QuestionCode,
            answer.QuestionLabel,
            answer.QuestionHint,
            (int)answer.QuestionType,
            answer.Step,
            answer.DisplayOrder,
            answer.Value,
            answer.DisplayValue,
            answer.OptionsSnapshot);
}

public record ServiceAnswerInputDto(
    string QuestionCode,
    string? Value);

public record ServiceAnswerWithQuestionDto(
    string QuestionCode,
    string? Value,
    string? DisplayValue);