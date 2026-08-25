using System.Text.Json;
using Domain.DataCollections.Forms;

namespace Api.Services.Implementation;

/// <summary>
/// Resolves the human-readable display text for a stored answer, preferring the
/// self-describing snapshot captured at submission time and falling back to the live field.
/// </summary>
public static class AnswerSnapshotResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed record OptionPair(string Value, string Label);

    public static string? ResolveDisplayValue(FormFieldValue? fieldValue, FormField? liveField)
    {
        if (fieldValue == null)
        {
            return null;
        }

        var value = fieldValue.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var optionMap = ParseSnapshotOptions(fieldValue.OptionsSnapshot) ?? BuildLiveOptionMap(liveField);
        if (optionMap == null || optionMap.Count == 0)
        {
            return fieldValue.DisplayValue ?? value;
        }

        // Single value (including option values that themselves contain commas).
        if (optionMap.TryGetValue(value.Trim(), out var single))
        {
            return single;
        }

        // Otherwise treat as a comma-separated multi-select (checkbox).
        var labels = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .Select(part => optionMap.TryGetValue(part, out var label) ? label : part);

        return string.Join(", ", labels);
    }

    /// <summary>
    /// The question label captured at save time, or null for rows that predate snapshotting
    /// (legacy or bulk-uploaded) so callers fall back to the live field.
    /// </summary>
    public static string? ResolveQuestionLabel(FormFieldValue? fieldValue) =>
        string.IsNullOrWhiteSpace(fieldValue?.QuestionLabel) ? null : fieldValue.QuestionLabel;

    /// <summary>
    /// The text to render as the question itself. Once a submission is locked it is history and has
    /// to read as it did when it was answered, whichever screen renders it - a locked collection is
    /// shown through the wizard as well as the summary, so this cannot be left to the client. While
    /// it is still editable the live label wins instead, so a correction made in Admin Configuration
    /// reaches the person who is about to answer.
    /// </summary>
    public static string ResolveDisplayLabel(FormFieldValue? fieldValue, FormField liveField, bool isLocked) =>
        isLocked ? ResolveQuestionLabel(fieldValue) ?? liveField.Label : liveField.Label;

    private static Dictionary<string, string>? ParseSnapshotOptions(string? optionsSnapshot)
    {
        if (string.IsNullOrWhiteSpace(optionsSnapshot))
        {
            return null;
        }

        try
        {
            var pairs = JsonSerializer.Deserialize<List<OptionPair>>(optionsSnapshot, JsonOptions);
            if (pairs == null)
            {
                return null;
            }

            var map = new Dictionary<string, string>();
            foreach (var pair in pairs)
            {
                map[pair.Value] = pair.Label;
            }

            return map;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Dictionary<string, string>? BuildLiveOptionMap(FormField? liveField)
    {
        if (liveField == null || liveField.Options.Count == 0)
        {
            return null;
        }

        var map = new Dictionary<string, string>();
        foreach (var option in liveField.Options)
        {
            map[option.Value] = option.Label;
        }

        return map;
    }
}