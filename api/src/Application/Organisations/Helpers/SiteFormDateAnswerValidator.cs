using Application.SiteForms.Dtos;
using Domain.SiteForms;

namespace Application.Organisations.Helpers;

public static class SiteFormDateAnswerValidator
{
    public static string? ValidateAnswers(
        IReadOnlyList<SiteAnswerInputDto> answers,
        IReadOnlyList<SiteFormQuestion> questions)
    {
        foreach (var answer in answers)
        {
            if (string.IsNullOrWhiteSpace(answer.Value))
            {
                continue;
            }

            var question = questions.FirstOrDefault(q => q.Code == answer.QuestionCode);
            if (question == null || !DateTime.TryParse(answer.Value, out var date))
            {
                continue;
            }

            var error = ValidateDateRule(question, date);
            if (error != null)
            {
                return error;
            }
        }

        return null;
    }

    public static string? ValidateDateRule(SiteFormQuestion question, DateTime date)
    {
        if (question.QuestionType != SiteFormQuestionType.Date || string.IsNullOrEmpty(question.DateValidationRule))
        {
            return null;
        }

        var today = DateTime.UtcNow.Date;

        if (question.DateValidationRule == SiteFormDateValidationRules.NotInFuture && date.Date > today)
        {
            return $"'{question.Label}' must not be a date in the future.";
        }

        if (question.DateValidationRule == SiteFormDateValidationRules.NotInPast && date.Date < today)
        {
            return $"'{question.Label}' must not be a date in the past.";
        }

        return null;
    }
}