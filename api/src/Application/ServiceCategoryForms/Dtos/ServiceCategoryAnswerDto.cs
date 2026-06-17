using Domain.ServiceCategoryForms;

namespace Application.ServiceCategoryForms.Dtos;

public record ServiceCategoryAnswerDto(
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
    public static ServiceCategoryAnswerDto FromDomainModel(ServiceCategoryAnswer answer) =>
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

public record ServiceCategoryAnswerInputDto(
    string QuestionCode,
    string? Value);