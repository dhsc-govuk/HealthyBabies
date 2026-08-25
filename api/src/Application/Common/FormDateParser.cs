using System.Globalization;

namespace Application.Common;

/// <summary>
/// Parses date answers exactly as the client emits them: DD/MM/YYYY, day first.
///
/// Never parse these values with a culture-sensitive <c>DateTime.Parse</c>/<c>TryParse</c> overload.
/// The API configures no culture, so the ambient culture resolves month-first on the Linux host and
/// "03/10/2026" (3 October) silently becomes 10 March — which is how BLUM-7521 rejected a valid
/// future date as being in the past. Day-first parsing is fixed here so it cannot drift with the
/// environment.
///
/// ISO (YYYY-MM-DD) is accepted as well: bulk-upload templates advertise it, and older rows may
/// carry it. The variants with a time component cover values produced by Excel cell formatting and
/// by legacy .NET <c>ToString()</c> round-trips. Every accepted format is day-first or ISO, so no
/// value is ambiguous.
/// </summary>
public static class FormDateParser
{
    private static readonly string[] AcceptedFormats =
    [
        "dd/MM/yyyy", "d/M/yyyy", "dd/M/yyyy", "d/MM/yyyy",
        "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.FFFFFFF", "yyyy-MM-dd HH:mm:ss",
        "dd/MM/yyyy HH:mm:ss", "d/M/yyyy HH:mm:ss", "d/M/yyyy H:mm:ss",
        "dd/MM/yyyy HH:mm", "d/M/yyyy HH:mm",
        "dd/MM/yyyy h:mm:ss tt", "d/M/yyyy h:mm:ss tt",

        // The bulk-upload grid accepts a dash separator and a year-first slash form, and validates
        // both client-side (Services/BulkUpload/validation.ts, Locations/BulkUpload/validation.ts).
        // The server has to accept exactly the same set or a row the grid marks valid is rejected
        // on submit with a message contradicting the one the grid just showed.
        "dd-MM-yyyy", "d-M-yyyy", "dd-M-yyyy", "d-MM-yyyy", "yyyy/MM/dd",

        // Bulk-upload cells are read as their displayed text, so a column formatted as a long
        // date arrives spelled out. These carry the month by name and so cannot be misordered.
        "d MMMM yyyy", "dd MMMM yyyy", "d MMM yyyy", "dd MMM yyyy",
    ];

    public static bool TryParse(string? value, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return DateTime.TryParseExact(
            value.Trim(), AcceptedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}