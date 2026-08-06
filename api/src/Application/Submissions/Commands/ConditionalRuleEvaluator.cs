using System.Globalization;
using System.Text.Json;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;

namespace Application.Submissions.Commands;

public static class ConditionalRuleEvaluator
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static bool IsFieldVisible(
        FormField field,
        Dictionary<string, string> rowData,
        List<FormField> allFields,
        Dictionary<string, string>? serviceAnswers = null)
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

            return EvaluateCondition(rules.ShowWhen, rowData, allFields, serviceAnswers);
        }
        catch
        {
            return true;
        }
    }

    public static bool EvaluateCondition(
        ShowWhenCondition condition,
        Dictionary<string, string> rowData,
        List<FormField> allFields,
        Dictionary<string, string>? serviceAnswers = null)
    {
        if (condition.AllOf != null && condition.AllOf.Any())
        {
            return condition.AllOf.All(c => EvaluateCondition(c, rowData, allFields, serviceAnswers));
        }

        if (condition.AnyOf != null && condition.AnyOf.Any())
        {
            return condition.AnyOf.Any(c => EvaluateCondition(c, rowData, allFields, serviceAnswers));
        }

        if (!string.IsNullOrEmpty(condition.ServiceFieldKey))
        {
            var serviceValue = serviceAnswers?.GetValueOrDefault(condition.ServiceFieldKey);

            var serviceNumericResult = EvaluateNumericOperators(condition, serviceValue);
            if (serviceNumericResult.HasValue)
            {
                return serviceNumericResult.Value;
            }

            if (condition.Equals != null)
            {
                return string.Equals(serviceValue, condition.Equals, StringComparison.OrdinalIgnoreCase);
            }

            if (condition.In != null && condition.In.Any())
            {
                return condition.In.Any(v => string.Equals(serviceValue, v, StringComparison.OrdinalIgnoreCase));
            }

            return true;
        }

        if (string.IsNullOrEmpty(condition.FieldKey))
        {
            return true;
        }

        var fieldValue = rowData.GetValueOrDefault(condition.FieldKey);

        var numericResult = EvaluateNumericOperators(condition, fieldValue);
        if (numericResult.HasValue)
        {
            return numericResult.Value;
        }

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

    /// <summary>
    /// Evaluates any numeric comparison operator present on the condition (gt/gte/lt/lte/eq/neq)
    /// against the supplied field value. Returns null when the condition carries no numeric operator,
    /// so the caller can fall through to the option-based (equals/in) checks.
    /// </summary>
    private static bool? EvaluateNumericOperators(ShowWhenCondition condition, string? value)
    {
        if (condition.Gt != null)
        {
            return CompareNumeric(value, condition.Gt, (a, b) => a > b);
        }

        if (condition.Gte != null)
        {
            return CompareNumeric(value, condition.Gte, (a, b) => a >= b);
        }

        if (condition.Lt != null)
        {
            return CompareNumeric(value, condition.Lt, (a, b) => a < b);
        }

        if (condition.Lte != null)
        {
            return CompareNumeric(value, condition.Lte, (a, b) => a <= b);
        }

        if (condition.Eq != null)
        {
            return CompareNumeric(value, condition.Eq, (a, b) => a.Equals(b));
        }

        if (condition.Neq != null)
        {
            return CompareNumeric(value, condition.Neq, (a, b) => !a.Equals(b));
        }

        return null;
    }

    private static bool CompareNumeric(string? actualRaw, string? thresholdRaw, Func<double, double, bool> comparer)
    {
        return double.TryParse(actualRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var actual)
            && double.TryParse(thresholdRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var threshold)
            && comparer(actual, threshold);
    }
}

public class ConditionalRules
{
    public ShowWhenCondition? ShowWhen { get; set; }
}

public class ShowWhenCondition
{
    public string? FieldKey { get; set; }
    public string? ServiceFieldKey { get; set; }
    public string? Equals { get; set; }
    public string? Gt { get; set; }
    public string? Gte { get; set; }
    public string? Lt { get; set; }
    public string? Lte { get; set; }
    public string? Eq { get; set; }
    public string? Neq { get; set; }
    public List<string>? In { get; set; }
    public List<ShowWhenCondition>? AllOf { get; set; }
    public List<ShowWhenCondition>? AnyOf { get; set; }
}