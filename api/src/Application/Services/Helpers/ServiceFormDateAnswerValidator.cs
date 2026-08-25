using Application.Common;
using Application.ServiceForms.Dtos;
using Domain.ServiceForms;

namespace Application.Services.Helpers;

public static class ServiceFormDateAnswerValidator
{
    public static string? ValidateAnswers(
        IReadOnlyList<ServiceAnswerInputDto> answers,
        IReadOnlyList<ServiceFormQuestion> questions,
        DateTime? today = null)
    {
        foreach (var answer in answers)
        {
            if (string.IsNullOrWhiteSpace(answer.Value))
            {
                continue;
            }

            var question = questions.FirstOrDefault(q => q.Code == answer.QuestionCode);
            if (question == null || !FormDateParser.TryParse(answer.Value, out var date))
            {
                continue;
            }

            var error = ValidateDateRule(question, date, today);
            if (error != null)
            {
                return error;
            }
        }

        return null;
    }

    public static string? ValidateDateRule(ServiceFormQuestion question, DateTime date, DateTime? today = null)
    {
        if (question.QuestionType != ServiceFormQuestionType.Date || string.IsNullOrEmpty(question.DateValidationRule))
        {
            return null;
        }

        var cutoff = (today ?? DateTime.UtcNow).Date;

        if (question.DateValidationRule == ServiceFormDateValidationRules.NotInFuture && date.Date > cutoff)
        {
            return $"'{question.Label}' must not be a date in the future.";
        }

        if (question.DateValidationRule == ServiceFormDateValidationRules.NotInPast && date.Date < cutoff)
        {
            return $"'{question.Label}' must not be a date in the past.";
        }

        return null;
    }
}