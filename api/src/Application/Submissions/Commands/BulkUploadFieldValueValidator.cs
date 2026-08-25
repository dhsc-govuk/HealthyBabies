using System.Text.Json;
using Application.Organisations.Helpers;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;

namespace Application.Submissions.Commands;

// Shared field-value validation for the Submissions bulk upload flow, used both at initial CSV
// upload (ValidateBulkUploadCommand) and at final persist time (BulkUploadRowProcessor), so a value
// that slips past the client's inline editor (or a caller that bypasses it entirely) is still
// rejected before it reaches a FormSubmission.
public static class BulkUploadFieldValueValidator
{
    public static List<string> ValidateFieldValue(string? value, FormField field, HashSet<string>? serviceNames = null)
    {
        var errors = new List<string>();

        if (field.IsRequired && string.IsNullOrWhiteSpace(value))
        {
            errors.Add("This field is required");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return errors;
        }

        var fieldTypeValue = field.FieldType.ToString().ToLowerInvariant();
        var hasDynamicOptions = !string.IsNullOrEmpty(field.Configuration) && field.Configuration.Contains("dynamicOptions");

        if (fieldTypeValue == FieldTypeConstants.Date)
        {
            // Per-cell only: the cross-field "notBefore" rule needs the whole row, so it is applied
            // by DeliveryPlanDateAnswerValidator at save time rather than here.
            if (!DeliveryPlanDateAnswerValidator.TryParse(value, out var parsedDate))
            {
                errors.Add("Must be a valid date in DD/MM/YYYY format");
            }
            else if (ParseDateRule(field.Configuration) == "not_in_future" && parsedDate.Date > DateTime.UtcNow.Date)
            {
                errors.Add("Must not be a date in the future");
            }
        }
        else if (fieldTypeValue == FieldTypeConstants.Number)
        {
            var trimmedValue = value.Trim();
            if (!decimal.TryParse(trimmedValue, out var numericValue))
            {
                errors.Add("Must be a valid number");
            }
            else
            {
                var min = ParseConfiguration(field.Configuration)?.Min;
                if (min.HasValue && numericValue < min.Value)
                {
                    errors.Add($"Must be {min.Value} or more");
                }
            }
        }
        else if (fieldTypeValue == FieldTypeConstants.Radio || fieldTypeValue == FieldTypeConstants.Select)
        {
            if (hasDynamicOptions && field.Configuration!.Contains("\"services\"") && serviceNames != null)
            {
                var trimmedValue = value.Trim().ToLowerInvariant();
                if (!serviceNames.Contains(trimmedValue))
                {
                    errors.Add($"Service '{value}' not found");
                }
            }
            else if (field.Options.Any())
            {
                var validOptions = field.Options.Select(o => o.Value.ToLowerInvariant()).ToHashSet();
                var validLabels = field.Options.Select(o => o.Label.ToLowerInvariant()).ToHashSet();

                var trimmedValue = value.Trim().ToLowerInvariant();
                if (!validOptions.Contains(trimmedValue) && !validLabels.Contains(trimmedValue))
                {
                    var optionList = string.Join(", ", field.Options.Select(o => o.Label));
                    errors.Add($"Invalid option. Valid options: {optionList}");
                }
            }
        }
        else if (fieldTypeValue == FieldTypeConstants.Checkbox)
        {
            // A dynamic-options checkbox has no seeded Options, so it must be checked against the
            // runtime list first — otherwise the Options.Any() guard below is false and every value,
            // valid or not, passes straight through.
            if (hasDynamicOptions && field.Configuration!.Contains("\"services\"") && serviceNames != null)
            {
                var selected = value.Split(',')
                    .Select(v => v.Trim())
                    .Where(v => !string.IsNullOrEmpty(v))
                    .ToList();

                var unknown = selected.Where(v => !serviceNames.Contains(v.ToLowerInvariant())).ToList();
                if (unknown.Any())
                {
                    errors.Add($"Service(s) not found: {string.Join(", ", unknown)}");
                }
            }
            else if (field.Options.Any())
            {
                var values = value.Split(',').Select(v => v.Trim().ToLowerInvariant()).Where(v => !string.IsNullOrEmpty(v)).ToList();
                var checkboxOptions = field.Options.Select(o => o.Value.ToLowerInvariant()).ToHashSet();
                var checkboxLabels = field.Options.Select(o => o.Label.ToLowerInvariant()).ToHashSet();

                var invalidValues = values.Where(v =>
                    !checkboxOptions.Contains(v) && !checkboxLabels.Contains(v)).ToList();

                if (invalidValues.Any())
                {
                    var optionList = string.Join(", ", field.Options.Select(o => o.Label));
                    errors.Add($"Invalid options: {string.Join(", ", invalidValues)}. Valid options: {optionList}");
                }
            }
        }

        return errors;
    }

    // Mirrors the client's validateSumGroups (BulkUpload/validation.ts): grouped demographic fields
    // (sumGroup) must not sum to more than their maxSumField, and single fields with a maxSumField but
    // no sumGroup (e.g. QSU14) must not individually exceed it.
    public static List<(string FieldKey, string Message)> ValidateSumGroups(
        Dictionary<string, string> rowData,
        IReadOnlyList<FormField> fields,
        Dictionary<string, string>? serviceAnswers = null)
    {
        var errors = new List<(string, string)>();
        var processedGroups = new HashSet<string>();

        foreach (var field in fields)
        {
            var config = ParseConfiguration(field.Configuration);
            if (config?.MaxSumField == null)
            {
                continue;
            }

            if (!ConditionalRuleEvaluator.IsFieldVisible(field, rowData, fields.ToList(), serviceAnswers))
            {
                continue;
            }

            if (!decimal.TryParse(rowData.GetValueOrDefault(config.MaxSumField), out var maxValue) || maxValue <= 0)
            {
                continue;
            }

            if (config.SumGroup != null)
            {
                if (!processedGroups.Add(config.SumGroup))
                {
                    continue;
                }

                var groupFields = fields.Where(f => ParseConfiguration(f.Configuration)?.SumGroup == config.SumGroup).ToList();

                decimal sum = 0;
                foreach (var groupField in groupFields)
                {
                    if (decimal.TryParse(rowData.GetValueOrDefault(groupField.FieldKey), out var groupValue))
                    {
                        sum += groupValue;
                    }
                }

                if (sum > maxValue)
                {
                    var groupLabel = config.GroupLabel ?? config.SumGroup;
                    var message = $"The total for '{groupLabel}' ({sum}) cannot exceed the total number of service users ({maxValue})";
                    foreach (var groupField in groupFields)
                    {
                        if (decimal.TryParse(rowData.GetValueOrDefault(groupField.FieldKey), out var groupValue) && groupValue > 0)
                        {
                            errors.Add((groupField.FieldKey, message));
                        }
                    }
                }
            }
            else if (decimal.TryParse(rowData.GetValueOrDefault(field.FieldKey), out var fieldValue) && fieldValue > maxValue)
            {
                errors.Add((field.FieldKey, $"This value ({fieldValue}) cannot exceed the total number of service users ({maxValue})"));
            }
        }

        return errors;
    }

    private static string? ParseDateRule(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(configuration);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("dateRule", out var rule)
                    ? rule.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static FieldConfiguration? ParseConfiguration(string? configuration)
    {
        if (string.IsNullOrEmpty(configuration))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<FieldConfiguration>(configuration, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private class FieldConfiguration
    {
        public int? Min { get; set; }
        public string? MaxSumField { get; set; }
        public string? SumGroup { get; set; }
        public string? GroupLabel { get; set; }
    }
}