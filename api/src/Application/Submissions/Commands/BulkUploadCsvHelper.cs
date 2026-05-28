using Application.Common;
using Domain.DataCollections.Forms;

namespace Application.Submissions.Commands;

internal static class BulkUploadCsvHelper
{
    public const string BulkUploadStagingContainer = "bulk-upload-staging";

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