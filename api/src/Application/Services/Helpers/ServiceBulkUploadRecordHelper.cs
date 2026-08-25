using System.Text.Json;
using Application.Common;
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
        var effectiveRecord = BuildEffectiveRecord(record, questions);

        foreach (var question in questions)
        {
            if (!IsVisible(question, effectiveRecord))
            {
                continue;
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
                if (!FormDateParser.TryParse(value, out var date))
                {
                    errors.Add(new BulkUploadRowValidationError(question.Code, "Invalid date format. Expected: YYYY-MM-DD or DD/MM/YYYY"));
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

        // Against the pruned record, so an age pair left behind by an abandoned branch cannot fail a row
        // over values ApplyAnswers is about to discard.
        var ageRangeError = ValidateAgeRange(effectiveRecord, questions);
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

    // Mirrors the client's getEffectiveServiceAnswers. A question whose own parent is hidden must not
    // be kept alive by that parent's leftover cell: with SMD08 set to infant_feeding, a stale
    // SMD10 = "Exemption (other)" column would otherwise make SMD10a required (BLUM-7221).
    // Ordered by step then display order so parents resolve first, and repeated until nothing more
    // drops, because the legacy "indent under" configuration permits forward and mutual references.
    // Each pass only ever removes keys, so it always terminates.
    private static Dictionary<string, string> BuildEffectiveRecord(
        Dictionary<string, string> record,
        IReadOnlyList<ServiceFormQuestion> questions)
    {
        var effective = new Dictionary<string, string>(record);
        var ordered = questions.OrderBy(q => q.Step).ThenBy(q => q.DisplayOrder).ToList();

        for (var pass = 0; pass < ordered.Count; pass++)
        {
            var dropped = false;
            foreach (var question in ordered)
            {
                if (!effective.ContainsKey(question.Code) || IsVisible(question, effective))
                {
                    continue;
                }

                effective.Remove(question.Code);
                dropped = true;
            }

            if (!dropped)
            {
                break;
            }
        }

        return effective;
    }

    // ConditionalRules is deliberately not evaluated here: the rich rule evaluator is bound to the
    // data collections FormField type and every service question currently ships with it unset.
    private static bool IsVisible(ServiceFormQuestion question, Dictionary<string, string> answers)
    {
        if (string.IsNullOrEmpty(question.ConditionalQuestionCode) || string.IsNullOrEmpty(question.ConditionalValue))
        {
            return true;
        }

        var parentSelected = SplitValues(answers.GetValueOrDefault(question.ConditionalQuestionCode, string.Empty));
        return SplitValues(question.ConditionalValue).Any(parentSelected.Contains);
    }

    // Checkbox answers and multi-value conditionals are both stored comma separated, so compare the
    // selected sets rather than the raw strings. Case insensitive to match the option validation above:
    // a cell of "Parenting" is accepted as the option "parenting", so it has to satisfy the conditional
    // too, or its children would be silently dropped by ApplyAnswers.
    private static HashSet<string> SplitValues(string value)
    {
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static void ApplyAnswers(
        Service service,
        Dictionary<string, string> record,
        IReadOnlyList<ServiceFormQuestion> questions)
    {
        service.ClearAnswersForStep(1);
        service.ClearAnswersForStep(2);

        var effectiveRecord = BuildEffectiveRecord(record, questions);

        foreach (var question in questions)
        {
            // Skipping hidden questions keeps the persisted answers consistent with what the form
            // would have accepted, instead of storing cells the uploader left behind.
            if (!IsVisible(question, effectiveRecord))
            {
                continue;
            }

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