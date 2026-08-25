using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;

namespace Application.Submissions.Helpers;

/// <summary>
/// The "valid options" row a downloaded bulk-upload template carries below its labels row, listing
/// each question's current option values so form-builder changes are visible in a plain CSV rather
/// than only in the Excel version's Options/Instructions sheets.
///
/// Written by the template generator and detected on upload
/// (BulkUploadCsvHelper.IsOptionsRow), so the format lives here rather than in either of them. The
/// same prefix is mirrored in the frontend parser, client/src/components/Global/BulkUpload/validation.ts.
/// </summary>
public static class BulkUploadOptionsRow
{
    public const string CellPrefix = "Options:";

    private const string MultiValueSuffix = " (one or more, comma-separated)";

    /// <summary>
    /// Builds one cell of the row, or an empty string for a question the user can't pick from a list.
    /// Only the values are listed — that's what has to be typed into the column; the question text
    /// is already on the labels row, and value/label pairs stay on the Excel Instructions sheet.
    /// </summary>
    public static string BuildCell(FormField field)
    {
        // Service-backed fields draw their options from the organisation's services, not the form
        // builder. Listing every service name here would dwarf the row, and the valid names are
        // already in the pre-filled ServiceName/PPS01 column below.
        if (!field.Options.Any(o => o.IsActive) || DynamicServiceOptions.IsServiceBacked(field))
        {
            return string.Empty;
        }

        var values = field.Options
            .Where(o => o.IsActive)
            .OrderBy(o => o.DisplayOrder)
            .Select(o => o.Value);

        var cell = $"{CellPrefix} {string.Join("|", values)}";

        return IsMultiValue(field) ? cell + MultiValueSuffix : cell;
    }

    private static bool IsMultiValue(FormField field) =>
        field.FieldType.Value is FieldTypeConstants.Checkbox or FieldTypeConstants.MultiSelect;
}