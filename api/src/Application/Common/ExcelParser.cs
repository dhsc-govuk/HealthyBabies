using Application.Common.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;

namespace Application.Common;

public class ExcelParser : IBulkUploadFileParser
{
    private static readonly string[] SupportedExtensions = [".xlsx", ".xls"];

    public bool CanParse(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return false;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public async Task<BulkUploadParseResult> ParseAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return new BulkUploadParseResult(false, [], "File is empty or null");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
        {
            return new BulkUploadParseResult(false, [], "File must be an Excel file (.xlsx or .xls)");
        }

        try
        {
            var records = new List<Dictionary<string, string>>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);

            // Try to find "Data" sheet first, otherwise use first visible sheet
            var worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == "Data")
                ?? workbook.Worksheets.FirstOrDefault(ws => ws.Visibility == XLWorksheetVisibility.Visible)
                ?? workbook.Worksheets.First();

            var headerRow = worksheet.Row(1);
            var headers = new List<string>();
            var lastColumnUsed = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

            for (var col = 1; col <= lastColumnUsed; col++)
            {
                var headerValue = headerRow.Cell(col).GetString().Trim();
                headers.Add(headerValue);
            }

            if (headers.Count == 0 || headers.All(string.IsNullOrWhiteSpace))
            {
                return new BulkUploadParseResult(false, [], "Excel file must contain a header row");
            }

            var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 0;

            for (var rowNum = 2; rowNum <= lastRowUsed; rowNum++)
            {
                var row = worksheet.Row(rowNum);
                var record = new Dictionary<string, string>();
                var hasData = false;

                for (var col = 0; col < headers.Count; col++)
                {
                    var header = headers[col];
                    if (string.IsNullOrWhiteSpace(header))
                    {
                        continue;
                    }

                    var cellValue = row.Cell(col + 1).GetString().Trim();
                    record[header] = cellValue;

                    if (!string.IsNullOrWhiteSpace(cellValue))
                    {
                        hasData = true;
                    }
                }

                if (hasData)
                {
                    record["_RowNumber"] = rowNum.ToString();
                    records.Add(record);
                }
            }

            return new BulkUploadParseResult(true, records);
        }
        catch (Exception ex)
        {
            return new BulkUploadParseResult(false, [], $"Error parsing Excel file: {ex.Message}");
        }
    }
}