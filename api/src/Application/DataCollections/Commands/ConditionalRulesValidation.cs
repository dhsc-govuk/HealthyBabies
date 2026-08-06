using System.Text.Json;
using Application.Submissions.Commands;
using Domain.DataCollections.Forms;

namespace Application.DataCollections.Commands;

public static class ConditionalRulesValidation
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static string? FindDuplicateFieldKey(string? conditionalRules)
    {
        var fieldKeys = ExtractFieldKeys(conditionalRules).ToList();
        return fieldKeys
            .GroupBy(k => k, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .FirstOrDefault();
    }

    public static string? FindForwardReferencedFieldKey(string? conditionalRules, int currentDisplayOrder, IReadOnlyList<FormField> moduleFields)
    {
        foreach (var fieldKey in ExtractFieldKeys(conditionalRules))
        {
            var referenced = moduleFields.FirstOrDefault(f => string.Equals(f.FieldKey, fieldKey, StringComparison.OrdinalIgnoreCase));
            if (referenced != null && referenced.DisplayOrder > currentDisplayOrder)
            {
                return fieldKey;
            }
        }

        return null;
    }

    private static IEnumerable<string> ExtractFieldKeys(string? conditionalRules)
    {
        if (string.IsNullOrEmpty(conditionalRules))
        {
            return [];
        }

        try
        {
            var rules = JsonSerializer.Deserialize<ConditionalRules>(conditionalRules, JsonOptions);
            return rules?.ShowWhen == null ? [] : ExtractFieldKeys(rules.ShowWhen);
        }
        catch
        {
            return [];
        }
    }

    private static IEnumerable<string> ExtractFieldKeys(ShowWhenCondition condition)
    {
        if (!string.IsNullOrEmpty(condition.FieldKey))
        {
            yield return condition.FieldKey;
        }

        foreach (var child in (condition.AllOf ?? []).Concat(condition.AnyOf ?? []))
        {
            foreach (var key in ExtractFieldKeys(child))
            {
                yield return key;
            }
        }
    }
}