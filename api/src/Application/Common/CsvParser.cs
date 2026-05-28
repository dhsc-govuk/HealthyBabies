using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Application.Common;

public class CsvParser : IBulkUploadFileParser
{
    private static readonly string[] SupportedExtensions = [".csv"];

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
        try
        {
            var records = await ParseCsvAsync(file);
            return new BulkUploadParseResult(true, records);
        }
        catch (ArgumentException ex)
        {
            return new BulkUploadParseResult(false, [], ex.Message);
        }
        catch (Exception ex)
        {
            return new BulkUploadParseResult(false, [], $"Error parsing CSV file: {ex.Message}");
        }
    }

    public static async Task<List<Dictionary<string, string>>> ParseCsvAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or null");
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("File must be a CSV file (.csv)");
        }

        var records = new List<Dictionary<string, string>>();

        using var reader = new StreamReader(file.OpenReadStream());
        var headerLine = await reader.ReadLineAsync();

        if (string.IsNullOrWhiteSpace(headerLine))
        {
            throw new ArgumentException("CSV file must contain a header row");
        }

        var headers = ParseCsvLine(headerLine);

        string? line;
        var rowNumber = 1;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            rowNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = ParseCsvLine(line);
            var record = new Dictionary<string, string>();

            for (int i = 0; i < Math.Min(headers.Length, values.Length); i++)
            {
                var header = headers[i].Trim().Trim('"');
                var value = values[i].Trim().Trim('"');
                record[header] = value;
            }

            // Add row number for error reporting
            record["_RowNumber"] = rowNumber.ToString();
            records.Add(record);
        }

        return records;
    }

    public static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var currentValue = string.Empty;
        var inQuotes = false;
        var i = 0;

        while (i < line.Length)
        {
            var character = line[i];

            if (character == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote (double quote becomes single quote)
                    currentValue += '"';
                    i += 2;
                }
                else
                {
                    // Toggle quote state
                    inQuotes = !inQuotes;
                    i++;
                }
            }
            else if (character == ',' && !inQuotes)
            {
                // End of field
                values.Add(currentValue);
                currentValue = string.Empty;
                i++;
            }
            else
            {
                currentValue += character;
                i++;
            }
        }

        // Add the last value
        values.Add(currentValue);
        return values.ToArray();
    }

    public static T? GetValue<T>(Dictionary<string, string> record, string columnName)
    {
        if (!record.TryGetValue(columnName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        try
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)value;
            }

            if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
            {
                if (bool.TryParse(value, out var boolResult))
                {
                    return (T)(object)boolResult;
                }

                return default;
            }

            if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
            {
                if (int.TryParse(value, out var intResult))
                {
                    return (T)(object)intResult;
                }

                return default;
            }

            if (typeof(T) == typeof(long) || typeof(T) == typeof(long?))
            {
                if (long.TryParse(value, out var longResult))
                {
                    return (T)(object)longResult;
                }

                return default;
            }

            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                if (DateTime.TryParse(value, out var dateResult))
                {
                    return (T)(object)dateResult;
                }

                return default;
            }

            // For other types, try to convert using Convert.ChangeType
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    public static void ValidateRequiredColumns(Dictionary<string, string> record, params string[] requiredColumns)
    {
        var missingColumns = requiredColumns.Where(col => !record.ContainsKey(col)).ToList();

        if (missingColumns.Any())
        {
            throw new ArgumentException($"Missing required columns: {string.Join(", ", missingColumns)}");
        }
    }

    public static void ValidateRequiredValues(Dictionary<string, string> record, params string[] requiredColumns)
    {
        var emptyColumns = requiredColumns
            .Where(col => !record.TryGetValue(col, out var value) || string.IsNullOrWhiteSpace(value))
            .ToList();

        if (emptyColumns.Any())
        {
            var rowNumber = record.GetValueOrDefault("_RowNumber", "unknown");
            throw new ArgumentException($"Row {rowNumber}: Missing required values for columns: {string.Join(", ", emptyColumns)}");
        }
    }
}

public static class DictionaryExtensions
{
    public static T GetValueOrDefault<T>(this Dictionary<string, string> record, string key, T defaultValue = default)
    {
        return record.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? CsvParser.GetValue<T>(record, key) ?? defaultValue
            : defaultValue;
    }
}