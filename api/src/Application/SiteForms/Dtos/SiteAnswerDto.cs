using Domain.SiteForms;

namespace Application.SiteForms.Dtos;

public record SiteAnswerDto(
    Guid Id,
    string QuestionCode,
    string QuestionLabel,
    string? QuestionHint,
    int QuestionType,
    int DisplayOrder,
    string? Value,
    string? DisplayValue,
    string? OptionsSnapshot)
{
    public static SiteAnswerDto FromDomainModel(SiteAnswer answer) =>
        new(
            answer.Id.Value,
            answer.QuestionCode,
            answer.QuestionLabel,
            answer.QuestionHint,
            (int)answer.QuestionType,
            answer.DisplayOrder,
            answer.Value,
            answer.DisplayValue,
            answer.OptionsSnapshot);
}