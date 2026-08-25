using System.Globalization;
using System.Text;
using ClosedXML.Excel;

// BLUM-7454 pilot tool: converts each LA's raw Delivery Plan workbook into the CSV shape
// BulkUploadRowProcessor expects (header row = FormField.FieldKey values), plus a gap report of
// every judgment call, default, or missing-source-column encountered - reviewed by a person before
// any CSV is POSTed to the ImportMigratedActivitiesCommand endpoint. Scoped to the "Delivery Grant"
// module only for now - the other 6 CSV-ready modules all have an "Expectations" checkbox field
// whose Excel wording doesn't match the app's seeded option labels, and that needs a confirmed
// crosswalk from the business before conversion can proceed for them (see
// BLUM-7454-expectations-crosswalk.md).

var workbooksDir = args.Length > 0 ? args[0] : @"C:\workspace\la-workbooks";
var outputDir = Path.Combine(workbooksDir, "output");
Directory.CreateDirectory(outputDir);

var las = new[]
{
    ("Manchester", "LA_Manchester_DP.xlsm"),
    ("Birmingham", "LA_Birmingham_DP.xlsm"),
    ("York", "LA_York_DP.xlsm"),
};

var gapReport = new GapReport();

foreach (var (laName, fileName) in las)
{
    var path = Path.Combine(workbooksDir, fileName);
    if (!File.Exists(path))
    {
        gapReport.AddFileLevel(laName, $"Workbook not found at '{path}' - skipped entirely.");
        continue;
    }

    var rows = DeliveryGrantConverter.Convert(path, laName, gapReport);
    var csvPath = Path.Combine(outputDir, $"{laName.ToLowerInvariant()}-delivery-grant.csv");
    CsvWriter.Write(csvPath, DeliveryGrantConverter.OutputHeaders, rows);
    Console.WriteLine($"{laName}: {rows.Count} activity rows -> {csvPath}");
}

var reportPath = Path.Combine(outputDir, "delivery-grant-gap-report.md");
File.WriteAllText(reportPath, gapReport.Render());
Console.WriteLine($"\nGap report -> {reportPath}");

internal static class DeliveryGrantConverter
{
    // Order matches how these fields are defined in DeliveryPlanFormModuleSeeder.PopulateDeliveryGrantFields.
    // DG_ID is excluded (app-generated on create); DG08/DG11 are excluded (Calculation fields, computed
    // server-side by BulkUploadRowProcessor - never accepted as input).
    public static readonly string[] OutputHeaders =
    [
        "DG_NAME", "DG_ABOUT", "DG01", "DG01a", "DG02", "DG03", "DG04", "DG05",
        "DG06", "DG06a", "DG06b", "DG07", "DG09", "DG10", "DG12", "DG13",
        "DG14", "DG15", "DG15a", "DG15b", "DG15c", "DG15d",
    ];

    private const string SheetName = "Delivery Grant";

    public static List<string[]> Convert(string workbookPath, string laName, GapReport gapReport)
    {
        using var workbook = new XLWorkbook(workbookPath);
        var sheet = workbook.Worksheet(SheetName);

        var headerRow = FindHeaderRow(sheet);
        if (headerRow == -1)
        {
            gapReport.AddFileLevel(laName, $"Could not locate the real header row on '{SheetName}' (expected a row containing both 'Activity ID' and 'Activity Status') - sheet skipped.");
            return [];
        }

        var columns = MapColumns(sheet, headerRow);
        if (!columns.ContainsKey("new / continuing for 2026-27"))
        {
            gapReport.AddModuleLevel(laName, "DG03 (New/Continuing) left blank for every row - source workbook has no 'New / Continuing for 2026-27' column on this sheet.");
        }

        var output = new List<string[]>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lastUsedRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;

        for (var r = headerRow + 1; r <= lastUsedRow; r++)
        {
            var get = (string header) => columns.TryGetValue(header, out var col) ? sheet.Cell(r, col).GetString().Trim() : null;

            var activityId = get("activity id");
            var nameAndDescription = get("activity name and description");
            var theme = get("overarching theme/area of focus");
            var forecast2627 = get("forecasted spend (2026-27)");

            if (string.IsNullOrWhiteSpace(nameAndDescription) && string.IsNullOrWhiteSpace(theme) && string.IsNullOrWhiteSpace(CleanCurrency(forecast2627)))
            {
                if (!string.IsNullOrWhiteSpace(activityId))
                {
                    gapReport.AddRowLevel(laName, activityId, "Row excluded - no name/description, theme, or spend figure (narrative-only or placeholder row, same rule as the spike's DG-001 exclusion).");
                }

                continue;
            }

            var (name, about) = SplitNameAndDescription(nameAndDescription ?? string.Empty);
            if (!seenNames.Add(name))
            {
                gapReport.AddRowLevel(laName, activityId ?? "(no id)", $"Derived DG_NAME '{name}' duplicates another row in this file - the app matches existing activities by name, so this row may silently overwrite the earlier one instead of creating a new activity.");
            }

            var risk = get("risk");
            var hasRisk = !string.IsNullOrWhiteSpace(risk);

            var newOrContinuing = get("new / continuing for 2026-27");

            var row = new[]
            {
                name,
                about,
                theme ?? string.Empty,
                string.Empty, // DG01a - never populated; only relevant when theme is "Other" and no source column exists for the extra detail.
                about, // DG02 - reused from the row's own description (see gap note below); source workbook is per-activity, not per-theme.
                newOrContinuing ?? string.Empty,
                get("activity status") ?? string.Empty,
                get("delivery status") ?? string.Empty,
                "no", // DG06 - Additional funding: no source column in any LA's workbook; defaults to "no" per the BLUM-7343 decision.
                string.Empty,
                string.Empty,
                CleanCurrency(forecast2627),
                CleanCurrency(get("actual spend (2026-27)")),
                CleanCurrency(get("committed spend (2026-27)")),
                CleanCurrency(get("forecasted spend (2027-28)")),
                CleanCurrency(get("forecasted spend (2028-29)")),
                get("change") ?? string.Empty,
                hasRisk ? "yes" : "no",
                risk ?? string.Empty,
                hasRisk ? get("likelihood") ?? string.Empty : string.Empty,
                hasRisk ? get("impact") ?? string.Empty : string.Empty,
                hasRisk ? get("mitigation") ?? string.Empty : string.Empty,
            };

            if (string.IsNullOrWhiteSpace(CleanCurrency(forecast2627)))
            {
                gapReport.AddRowLevel(laName, activityId ?? name, "DG07 (Forecasted Spend 2026-27) is required but blank in source - row will save as Draft, not Approved.");
            }

            output.Add(row);
        }

        return output;
    }

    // The sheet has two rows that both look like a header ("Activity ID" + "Activity Status"): a
    // short recap near the top, and the real one further down (immediately after the "Activity
    // Details / Financial Information" section super-header). Only the real one also splits Risk
    // into separate "Likelihood"/"Impact" columns - the recap merges them into "Risk Matrix" - so
    // require that too, and take the last match in range rather than the first, in case the pattern
    // repeats again.
    private static int FindHeaderRow(IXLWorksheet sheet)
    {
        var lastRow = Math.Min(sheet.LastRowUsed()?.RowNumber() ?? 0, 15);
        var found = -1;
        for (var r = 1; r <= lastRow; r++)
        {
            var cells = sheet.Row(r).CellsUsed().Select(c => c.GetString().Trim().ToLowerInvariant()).ToHashSet();
            if (cells.Contains("activity id") && cells.Contains("activity status") && cells.Contains("likelihood") && cells.Contains("impact"))
            {
                found = r;
            }
        }

        return found;
    }

    private static Dictionary<string, int> MapColumns(IXLWorksheet sheet, int headerRow)
    {
        var map = new Dictionary<string, int>();
        foreach (var cell in sheet.Row(headerRow).CellsUsed())
        {
            var header = cell.GetString().Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(header) && !map.ContainsKey(header))
            {
                map[header] = cell.Address.ColumnNumber;
            }
        }

        return map;
    }

    // The app has separate required DG_NAME (short, must be unique) and optional DG_ABOUT (long) fields;
    // the source workbook has one combined free-text column per activity. Judgment call: take up to the
    // first sentence break (period, or first 120 characters) as the name, and always keep the full
    // original text as the description so nothing is lost even when the derived name reads awkwardly.
    private static (string Name, string About) SplitNameAndDescription(string combined)
    {
        var trimmed = combined.Trim();
        if (trimmed.Length == 0)
        {
            return ("(unnamed activity)", string.Empty);
        }

        var periodIndex = trimmed.IndexOf('.');
        string name;
        if (periodIndex > 10 && periodIndex < 120)
        {
            name = trimmed[..periodIndex].Trim();
        }
        else if (trimmed.Length <= 120)
        {
            name = trimmed;
        }
        else
        {
            name = trimmed[..120].Trim() + "...";
        }

        return (name, trimmed);
    }

    // Currency cells render via ClosedXML's GetString() with their display format applied (e.g.
    // "£350,000.00"), but decimal.TryParse (used both by BulkUploadFieldValueValidator and the
    // Calculation-field FormulaEvaluator, both via the default NumberStyles.Number) rejects the "£"
    // symbol outright - so every spend figure must be stripped down to a plain decimal string before
    // it reaches the CSV, or the server-computed Planned Spend/Total Activity Budget figures silently
    // treat that value as zero.
    private static string CleanCurrency(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var cleaned = raw.Replace("£", string.Empty).Replace(",", string.Empty).Trim();
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value.ToString(CultureInfo.InvariantCulture)
            : string.Empty;
    }
}

internal static class CsvWriter
{
    public static void Write(string path, string[] headers, List<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(Quote)));
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", row.Select(Quote)));
        }

        File.WriteAllText(path, sb.ToString());
    }

    private static string Quote(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}

internal sealed class GapReport
{
    private readonly List<string> _fileLevel = [];
    private readonly List<string> _moduleLevel = [];
    private readonly List<string> _rowLevel = [];

    public void AddFileLevel(string laName, string message) => _fileLevel.Add($"**{laName}**: {message}");

    public void AddModuleLevel(string laName, string message) => _moduleLevel.Add($"**{laName}**: {message}");

    public void AddRowLevel(string laName, string activityRef, string message) => _rowLevel.Add($"**{laName} / {activityRef}**: {message}");

    public string Render()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Delivery Grant conversion — gap report");
        sb.AppendLine();
        sb.AppendLine("Generated by DeliveryPlanMigrationConverter. Review before using the CSVs against the migration endpoint.");
        sb.AppendLine();
        sb.AppendLine("## File-level issues");
        sb.AppendLine(_fileLevel.Count == 0 ? "None." : string.Join("\n", _fileLevel.Select(m => "- " + m)));
        sb.AppendLine();
        sb.AppendLine("## Module-wide defaults/assumptions");
        sb.AppendLine(_moduleLevel.Count == 0 ? "None." : string.Join("\n", _moduleLevel.Select(m => "- " + m)));
        sb.AppendLine();
        sb.AppendLine($"## Per-row findings ({_rowLevel.Count})");
        sb.AppendLine(_rowLevel.Count == 0 ? "None." : string.Join("\n", _rowLevel.Select(m => "- " + m)));
        return sb.ToString();
    }
}
