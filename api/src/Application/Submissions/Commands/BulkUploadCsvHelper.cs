using Application.Common;
using Application.Submissions.Helpers;
using Domain.DataCollections.Forms;

namespace Application.Submissions.Commands;

internal static class BulkUploadCsvHelper
{
    public const string BulkUploadStagingContainer = "bulk-upload-staging";

    // Header row aside, a downloaded template carries at most three documentation rows: labels,
    // valid options, and R/O indicators.
    private const int MaxPreambleRows = 3;

    public static async Task<List<List<string>>> ParseCsvAsync(Stream stream, CancellationToken cancellationToken)
    {
        var rows = new List<List<string>>();

        using var reader = new StreamReader(stream, leaveOpen: true);
        string? line;

        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = CsvParser.ParseCsvLine(line);
            rows.Add(values.ToList());
        }

        return rows;
    }

    public static bool IsLabelRow(List<string> row, List<FormField> fields)
    {
        var labelMatches = 0;
        foreach (var cell in row)
        {
            if (fields.Any(f => f.Label.Equals(cell, StringComparison.OrdinalIgnoreCase)))
            {
                labelMatches++;
            }
        }

        return labelMatches > row.Count / 2;
    }

    public static bool IsRequiredIndicatorsRow(List<string> row)
    {
        var roCount = 0;
        foreach (var cell in row)
        {
            var trimmed = cell.Trim().ToUpperInvariant();
            if (trimmed == "R" || trimmed == "O")
            {
                roCount++;
            }
        }

        return roCount > row.Count / 2;
    }

    // The template lists each question's valid option values in a row below the labels, so a plain
    // CSV shows form-builder options without opening the Excel version. It's documentation, never
    // data: every populated cell carries the prefix, which no genuine answer does.
    public static bool IsOptionsRow(List<string> row)
    {
        var populatedCells = 0;
        var optionCells = 0;

        foreach (var cell in row)
        {
            var trimmed = cell.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            populatedCells++;
            if (trimmed.StartsWith(BulkUploadOptionsRow.CellPrefix, StringComparison.OrdinalIgnoreCase))
            {
                optionCells++;
            }
        }

        return populatedCells > 0 && populatedCells == optionCells;
    }

    // Users hand-build CSVs with any subset of the documentation rows, so classify each row rather
    // than assuming a fixed order. Capped at the number the template itself emits so a run of
    // genuinely empty-looking data rows can't be swallowed.
    public static int CountPreambleRows(List<List<string>> dataRows, List<FormField> fields)
    {
        var count = 0;

        while (count < dataRows.Count && count < MaxPreambleRows)
        {
            var row = dataRows[count];
            if (!IsLabelRow(row, fields) && !IsOptionsRow(row) && !IsRequiredIndicatorsRow(row))
            {
                break;
            }

            count++;
        }

        return count;
    }

    public static Dictionary<string, string> MapRowToFields(List<string> headers, List<string> row)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < headers.Count && i < row.Count; i++)
        {
            var header = headers[i].Trim();
            var value = row[i].Trim();

            if (!string.IsNullOrEmpty(header))
            {
                result[header] = value;
            }
        }

        return result;
    }
}