using System.Text.Json;
using Application.Common;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;

namespace Application.Organisations.Helpers;

/// <summary>
/// Validates <see cref="FieldTypeConstants.Date"/> answers on delivery-plan activity forms.
///
/// Two rules are supported, both declared in the field's Configuration JSON so they stay data-driven
/// rather than hard-coded per strand:
///   {"dateRule":"not_in_future"} — the date must be today or earlier (actual start/completion dates).
///   {"notBefore":"PN06"}        — the date must not fall before the answer to another field in the
///                                 same module (planned completion cannot precede planned start).
///
/// Dates are stored as the client emits them, DD/MM/YYYY, so parsing is culture-invariant and
/// day-first — see <see cref="FormDateParser"/>, which every form path shares.
/// </summary>
public static class DeliveryPlanDateAnswerValidator
{
    public static bool TryParse(string? value, out DateTime date) => FormDateParser.TryParse(value, out date);

    /// <summary>
    /// Returns the first validation error across every date field in the module, or null when all pass.
    /// <paramref name="answers"/> is keyed by field key and holds the values as they will be persisted.
    /// </summary>
    public static string? Validate(
        IReadOnlyDictionary<string, string?> answers,
        IReadOnlyCollection<FormField> fields,
        DateTime? today = null)
    {
        var cutoff = (today ?? DateTime.UtcNow).Date;

        foreach (var field in fields.Where(f => f.FieldType.Value == FieldTypeConstants.Date))
        {
            if (!answers.TryGetValue(field.FieldKey, out var raw) || string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (!TryParse(raw, out var date))
            {
                return $"'{field.Label}' must be a real date in DD/MM/YYYY format.";
            }

            var (dateRule, notBefore) = ReadRules(field.Configuration);

            if (dateRule == "not_in_future" && date.Date > cutoff)
            {
                return $"'{field.Label}' must not be a date in the future.";
            }

            if (dateRule == "not_in_past" && date.Date < cutoff)
            {
                return $"'{field.Label}' must not be a date in the past.";
            }

            if (notBefore is null)
            {
                continue;
            }

            // A missing or unparseable comparison value is not this field's error to report —
            // the other field's own required/format check owns that.
            if (!answers.TryGetValue(notBefore, out var otherRaw) || !TryParse(otherRaw, out var otherDate))
            {
                continue;
            }

            if (date.Date < otherDate.Date)
            {
                var otherLabel = fields.FirstOrDefault(f => f.FieldKey == notBefore)?.Label ?? notBefore;
                return $"'{field.Label}' cannot be before '{otherLabel}'.";
            }
        }

        return null;
    }

    private static (string? DateRule, string? NotBefore) ReadRules(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
        {
            return (null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(configuration);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return (null, null);
            }

            var rule = root.TryGetProperty("dateRule", out var r) ? r.GetString() : null;
            var before = root.TryGetProperty("notBefore", out var b) ? b.GetString() : null;
            return (rule, before);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }
}