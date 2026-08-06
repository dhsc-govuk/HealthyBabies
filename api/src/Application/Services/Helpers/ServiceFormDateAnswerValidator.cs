using Application.ServiceForms.Dtos;
using Domain.ServiceForms;

namespace Application.Services.Helpers;

public static class ServiceFormDateAnswerValidator
{
    public static string? ValidateAnswers(
        IReadOnlyList<ServiceAnswerInputDto> answers,
        IReadOnlyList<ServiceFormQuestion> questions)
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

    public static string? ValidateDateRule(ServiceFormQuestion question, DateTime date)
    {
        if (question.QuestionType != ServiceFormQuestionType.Date || string.IsNullOrEmpty(question.DateValidationRule))
        {
            return null;
        }

        var today = DateTime.UtcNow.Date;

        if (question.DateValidationRule == ServiceFormDateValidationRules.NotInFuture && date.Date > today)
        {
            return $"'{question.Label}' must not be a date in the future.";
        }

        if (question.DateValidationRule == ServiceFormDateValidationRules.NotInPast && date.Date < today)
        {
            return $"'{question.Label}' must not be a date in the past.";
        }

        return null;
    }
}