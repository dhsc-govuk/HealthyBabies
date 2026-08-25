using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;

namespace Application.Submissions.Helpers;

/// <summary>
/// The questions a bulk-upload template has a column for, in column order.
///
/// One source of truth on purpose. The generator writes these columns, validate checks them, and
/// the parsers match the labels row against them to work out where the data starts. The row split
/// is what the client's row indices — and therefore its inline cell edits and row selection — are
/// keyed to, so a handler working from a different list would silently apply an edit to the wrong
/// record. Note this is NOT the list to hand to BulkUploadRowProcessor: that needs the fields
/// excluded below in order to generate activity IDs and auto-total the calculation fields.
/// </summary>
public static class BulkUploadTemplateColumns
{
    public static List<FormField> ForModule(DataCollectionFormModule formModule)
    {
        var ordered = formModule.Fields
            .Where(f => f.IsActive)
            .OrderBy(f => formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber ?? 0)
            .ThenBy(f => f.DisplayOrder)
            .ToList();

        // Activity modules (Delivery Plan strands) auto-generate their ID field and auto-total their
        // Calculation fields — neither is ever user-supplied, so neither gets a column.
        return formModule.AllowsMultipleActivities
            ? ordered.Where(f => f.FieldType.Value != FieldTypeConstants.ReadOnly && f.FieldType.Value != FieldTypeConstants.Calculation).ToList()
            : ordered;
    }
}