using System.Text.Json;
using Domain.DataCollections.Forms;

namespace Application.Submissions.Helpers;

/// <summary>
/// Helpers for fields whose options are the organisation's services rather than a seeded list,
/// declared as {"dynamicOptions":"services"} in the field's Configuration.
///
/// These fields store service IDs but are entered and displayed as service names, so a rename
/// never orphans an answer. Single-value (Outcome Scores' PPS01, a Select) and multi-value
/// (the delivery plan's "Which service is the activity funding?", a Checkbox) both use this.
/// </summary>
public static class DynamicServiceOptions
{
    public const string ServicesSource = "services";

    public static bool IsServiceBacked(FormField field) => IsServiceBacked(field.Configuration);

    public static bool IsServiceBacked(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(configuration);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("dynamicOptions", out var source)
                && string.Equals(source.GetString(), ServicesSource, StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Turns a stored answer — one service ID, or several separated by commas — into display names.
    /// An ID with no match is passed through unchanged rather than dropped, so a deleted service
    /// shows as its raw value instead of silently vanishing from an export.
    /// </summary>
    public static string ResolveDisplayValue(string? answer, IReadOnlyDictionary<string, string> serviceNamesById)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return string.Empty;
        }

        var resolved = answer
            .Split(',')
            .Select(part => part.Trim())
            .Where(part => !string.IsNullOrEmpty(part))
            .Select(part => serviceNamesById.TryGetValue(part, out var name) ? name : part);

        return string.Join(", ", resolved);
    }
}