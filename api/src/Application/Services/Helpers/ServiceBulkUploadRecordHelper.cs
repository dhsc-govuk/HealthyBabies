using System.Text.Json;
using Application.Services.Dtos;
using Domain.ServiceForms;
using Domain.Services;

namespace Application.Services.Helpers;

public static class ServiceBulkUploadRecordHelper
{
    public const string PredefinedNameCode = "SMD01";

    private const string LowestAgeCode = "SMD13";
    private const string HighestAgeCode = "SMD14";
    private const string UnknownOptionValue = "Unknown";

    public static List<BulkUploadRowValidationError> ValidateRecord(
        Dictionary<string, string> record,
        IReadOnlyList<ServiceFormQuestion> questions)
    {
        var errors = new List<BulkUploadRowValidationError>();

        foreach (var question in questions)
        {
            if (!string.IsNullOrEmpty(question.ConditionalQuestionCode))
            {
                var parentValue = record.GetValueOrDefault(question.ConditionalQuestionCode, string.Empty);
                if (parentValue != question.ConditionalValue)
                {
                    continue;
                }
            }

            var value = record.GetValueOrDefault(question.Code, string.Empty);

            if (question.IsRequired && string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new BulkUploadRowValidationError(question.Code, $"{question.Label} is required"));
                continue;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (question.QuestionType == ServiceFormQuestionType.Date)
            {
                if (!DateTime.TryParse(value, out var date))
                {
                    errors.Add(new BulkUploadRowValidationError(question.Code, "Invalid date format. Expected: YYYY-MM-DD"));
                    continue;
                }

                var dateError = ServiceFormDateAnswerValidator.ValidateDateRule(question, date);
                if (dateError != null)
                {
                    errors.Add(new BulkUploadRowValidationError(question.Code, dateError));
                }

                continue;
            }

            if (!question.Options.Any())
            {
                continue;
            }

            var validValues = question.Options.Select(o => o.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (question.QuestionType == ServiceFormQuestionType.Checkbox)
            {
                var selectedValues = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim())
                    .ToList();

                foreach (var selectedValue in selectedValues)
                {
                    if (!validValues.Contains(selectedValue))
                    {
                        var validOptions = string.Join(", ", question.Options.Select(o => o.Value));
                        errors.Add(new BulkUploadRowValidationError(
                            question.Code,
                            $"Invalid option '{selectedValue}'. Valid options: {validOptions}"));
                    }
                }
            }
            else if (question.QuestionType is ServiceFormQuestionType.Radio or ServiceFormQuestionType.Select)
            {
                if (!validValues.Contains(value))
                {
                    var validOptions = string.Join(", ", question.Options.Select(o => o.Value));
                    errors.Add(new BulkUploadRowValidationError(
                        question.Code,
                        $"Invalid option '{value}'. Valid options: {validOptions}"));
                }
            }
        }

        var ageRangeError = ValidateAgeRange(record, questions);
        if (ageRangeError != null)
        {
            errors.Add(ageRangeError);
        }

        return errors;
    }

    private static BulkUploadRowValidationError? ValidateAgeRange(
        Dictionary<string, string> record,
        IReadOnlyList<ServiceFormQuestion> questions)
    {
        var lowestValue = record.GetValueOrDefault(LowestAgeCode, string.Empty);
        var highestValue = record.GetValueOrDefault(HighestAgeCode, string.Empty);

        if (string.IsNullOrWhiteSpace(lowestValue) || string.IsNullOrWhiteSpace(highestValue))
        {
            return null;
        }

        if (lowestValue.Equals(UnknownOptionValue, StringComparison.OrdinalIgnoreCase) ||
            highestValue.Equals(UnknownOptionValue, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var lowestQuestion = questions.FirstOrDefault(q => q.Code == LowestAgeCode);
        var highestQuestion = questions.FirstOrDefault(q => q.Code == HighestAgeCode);

        var lowestOption = lowestQuestion?.Options.FirstOrDefault(o => o.Value.Equals(lowestValue, StringComparison.OrdinalIgnoreCase));
        var highestOption = highestQuestion?.Options.FirstOrDefault(o => o.Value.Equals(highestValue, StringComparison.OrdinalIgnoreCase));

        if (lowestOption == null || highestOption == null)
        {
            return null;
        }

        if (lowestOption.DisplayOrder > highestOption.DisplayOrder)
        {
            return new BulkUploadRowValidationError(
                HighestAgeCode,
                $"'{highestQuestion!.Label}' cannot be lower than '{lowestQuestion!.Label}'.");
        }

        return null;
    }

    public static void ApplyAnswers(
        Service service,
        Dictionary<string, string> record,
        IReadOnlyList<ServiceFormQuestion> questions)
    {
        service.ClearAnswersForStep(1);
        service.ClearAnswersForStep(2);

        foreach (var question in questions)
        {
            var value = record.GetValueOrDefault(question.Code, string.Empty);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var displayValue = GetDisplayValue(question, value);
            var optionsSnapshot = question.Options.Any()
                ? JsonSerializer.Serialize(question.Options.Select(o => new { o.Value, o.Label }).ToList())
                : null;

            service.AddAnswer(
                question.Code,
                question.Label,
                question.Hint,
                question.QuestionType,
                question.Step,
                question.DisplayOrder,
                value,
                displayValue,
                optionsSnapshot);
        }
    }

    public static string? GetDisplayValue(ServiceFormQuestion question, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (question.QuestionType == ServiceFormQuestionType.Checkbox)
        {
            var values = value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim());
            var labels = values
                .Select(v => question.Options.FirstOrDefault(o => o.Value.Equals(v, StringComparison.OrdinalIgnoreCase))?.Label ?? v)
                .ToList();
            return string.Join(", ", labels);
        }

        if (question.Options.Any())
        {
            var option = question.Options.FirstOrDefault(o => o.Value.Equals(value, StringComparison.OrdinalIgnoreCase));
            return option?.Label ?? value;
        }

        return value;
    }
}