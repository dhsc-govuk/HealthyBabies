using Application.ServiceForms.Dtos;
using Domain.ServiceForms;
using Domain.Services;

namespace Application.Services.Helpers;

public static class ServiceAgeRangeAnswerValidator
{
    private const string LowestAgeCode = "SMD13";
    private const string HighestAgeCode = "SMD14";
    private const string UnknownOptionValue = "Unknown";

    public static string? ValidateAnswers(
        Service service,
        IReadOnlyList<ServiceAnswerInputDto> answers,
        IReadOnlyList<ServiceFormQuestion> questions)
    {
        var lowestValue = GetEffectiveValue(service, answers, LowestAgeCode);
        var highestValue = GetEffectiveValue(service, answers, HighestAgeCode);

        if (string.IsNullOrWhiteSpace(lowestValue) || string.IsNullOrWhiteSpace(highestValue))
        {
            return null;
        }

        if (lowestValue.Equals(UnknownOptionValue, StringComparison.OrdinalIgnoreCase) ||
            highestValue.Equals(UnknownOptionValue, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var lowestQuestion = questions.FirstOrDefault(q => q.Code == LowestAgeCode && q.QuestionType == ServiceFormQuestionType.Radio);
        var highestQuestion = questions.FirstOrDefault(q => q.Code == HighestAgeCode && q.QuestionType == ServiceFormQuestionType.Radio);

        if (lowestQuestion == null || highestQuestion == null)
        {
            return null;
        }

        var lowestOption = lowestQuestion.Options.FirstOrDefault(o => o.Value.Equals(lowestValue, StringComparison.OrdinalIgnoreCase));
        var highestOption = highestQuestion.Options.FirstOrDefault(o => o.Value.Equals(highestValue, StringComparison.OrdinalIgnoreCase));

        if (lowestOption == null || highestOption == null)
        {
            return null;
        }

        if (lowestOption.DisplayOrder > highestOption.DisplayOrder)
        {
            return $"'{highestQuestion.Label}' cannot be lower than '{lowestQuestion.Label}'.";
        }

        return null;
    }

    private static string? GetEffectiveValue(
        Service service,
        IReadOnlyList<ServiceAnswerInputDto> answers,
        string questionCode)
    {
        var incoming = answers.FirstOrDefault(a => a.QuestionCode == questionCode);
        if (incoming != null)
        {
            return incoming.Value;
        }

        return service.GetAnswer(questionCode)?.Value;
    }
}