using System.Text.Json;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;

namespace Application.Submissions.Commands;

public static class ConditionalRuleEvaluator
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static bool IsFieldVisible(FormField field, Dictionary<string, string> rowData, List<FormField> allFields)
    {
        if (string.IsNullOrEmpty(field.ConditionalRules))
        {
            return true;
        }

        try
        {
            var rules = JsonSerializer.Deserialize<ConditionalRules>(field.ConditionalRules, JsonOptions);
            if (rules?.ShowWhen == null)
            {
                return true;
            }

            return EvaluateCondition(rules.ShowWhen, rowData, allFields);
        }
        catch
        {
            return true;
        }
    }

    public static bool EvaluateCondition(ShowWhenCondition condition, Dictionary<string, string> rowData, List<FormField> allFields)
    {
        if (condition.AllOf != null && condition.AllOf.Any())
        {
            return condition.AllOf.All(c => EvaluateCondition(c, rowData, allFields));
        }

        if (condition.AnyOf != null && condition.AnyOf.Any())
        {
            return condition.AnyOf.Any(c => EvaluateCondition(c, rowData, allFields));
        }

        if (string.IsNullOrEmpty(condition.FieldKey))
        {
            return true;
        }

        var fieldValue = rowData.GetValueOrDefault(condition.FieldKey);
        var parentField = allFields.FirstOrDefault(f => string.Equals(f.FieldKey, condition.FieldKey, StringComparison.OrdinalIgnoreCase));
        var normalizedTokens = NormalizeFieldValueToOptionValues(fieldValue, parentField);

        if (condition.Equals != null)
        {
            return normalizedTokens.Any(t => string.Equals(t, condition.Equals, StringComparison.OrdinalIgnoreCase));
        }

        if (condition.In != null && condition.In.Any())
        {
            return condition.In.Any(v => normalizedTokens.Any(t => string.Equals(t, v, StringComparison.OrdinalIgnoreCase)));
        }

        return true;
    }

    public static List<string> NormalizeFieldValueToOptionValues(string? fieldValue, FormField? parentField)
    {
        if (string.IsNullOrWhiteSpace(fieldValue))
        {
            return new List<string>();
        }

        var isMultiSelect = parentField != null &&
                            (parentField.FieldType.Value == FieldTypeConstants.Checkbox ||
                             parentField.FieldType.Value == FieldTypeConstants.MultiSelect);

        var tokens = isMultiSelect
            ? fieldValue.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList()
            : new List<string> { fieldValue.Trim() };

        if (parentField == null || !parentField.Options.Any())
        {
            return tokens;
        }

        return tokens
            .Select(token =>
            {
                var match = parentField.Options.FirstOrDefault(o =>
                    string.Equals(o.Value, token, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(o.Label, token, StringComparison.OrdinalIgnoreCase));
                return match?.Value ?? token;
            })
            .ToList();
    }
}

public class ConditionalRules
{
    public ShowWhenCondition? ShowWhen { get; set; }
}

public class ShowWhenCondition
{
    public string? FieldKey { get; set; }
    public string? Equals { get; set; }
    public List<string>? In { get; set; }
    public List<ShowWhenCondition>? AllOf { get; set; }
    public List<ShowWhenCondition>? AnyOf { get; set; }
}