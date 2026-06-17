using System.Text;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using ClosedXML.Excel;
using Domain.ServiceForms;

namespace Api.Services.Implementation;

public class ServicesBulkUploadTemplateService(
    IServiceFormQuestionQueries serviceFormQuestionQueries) : IServicesBulkUploadTemplateService
{
    public async Task<byte[]> GenerateCsvTemplateAsync(CancellationToken cancellationToken = default)
    {
        var questions = await serviceFormQuestionQueries.GetAllActive(cancellationToken);
        var orderedQuestions = questions
            .OrderBy(q => q.Step)
            .ThenBy(q => q.DisplayOrder)
            .ToList();

        var sb = new StringBuilder();

        // Row 1: Header row with question codes
        var headers = orderedQuestions.Select(q => EscapeCsvField(q.Code)).ToList();
        sb.AppendLine(string.Join(",", headers));

        // Row 2: Labels row with question labels
        var labels = orderedQuestions.Select(q => EscapeCsvField(q.Label)).ToList();
        sb.AppendLine(string.Join(",", labels));

        // Row 3+: Empty data row (placeholder for user data)
        var emptyRow = orderedQuestions.Select(_ => string.Empty).ToList();
        sb.AppendLine(string.Join(",", emptyRow));

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return field;
        }

        // If field contains comma, quote, or newline, wrap in quotes and escape internal quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    public async Task<byte[]> GenerateExcelTemplateAsync(CancellationToken cancellationToken = default)
    {
        var questions = await serviceFormQuestionQueries.GetAllActive(cancellationToken);
        var orderedQuestions = questions
            .OrderBy(q => q.Step)
            .ThenBy(q => q.DisplayOrder)
            .ToList();

        using var workbook = new XLWorkbook();

        // Create hidden options sheet FIRST (needed for named ranges)
        var optionsSheet = workbook.Worksheets.Add("Options");
        CreateOptionsSheet(optionsSheet, orderedQuestions, workbook);
        optionsSheet.Hide();

        // Create main data sheet (uses named ranges from Options sheet)
        var dataSheet = workbook.Worksheets.Add("Data");
        CreateDataSheet(dataSheet, orderedQuestions, workbook);

        // Create instructions sheet
        var instructionsSheet = workbook.Worksheets.Add("Instructions");
        CreateInstructionsSheet(instructionsSheet, orderedQuestions);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void CreateDataSheet(IXLWorksheet sheet, List<ServiceFormQuestion> questions, IXLWorkbook workbook)
    {
        // Row 1: Header row with question codes
        for (var i = 0; i < questions.Count; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = questions[i].Code;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Row 2: Labels row with question labels (like CSV has)
        for (var i = 0; i < questions.Count; i++)
        {
            sheet.Cell(2, i + 1).Value = questions[i].Label;
        }

        // Add data validation for dropdown columns
        for (var i = 0; i < questions.Count; i++)
        {
            var question = questions[i];
            var columnLetter = GetColumnLetter(i + 1);

            if (question.Code == "SMD19")
            {
                // SMD19 has no static options — site names come from the org's registered locations at runtime.
                // Add a comment so users see what to type when hovering the column header.
                var smd19Comment = sheet.Cell(1, i + 1).CreateComment();
                smd19Comment.AddText("Multi-select: enter the names of your organisation's registered delivery sites, comma-separated (e.g., \"Family Hub North, Family Hub South\"). Names must match the sites registered in your locations list.");
            }
            else if (question.Options.Any())
            {
                var optionValues = question.Options
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => o.Value)
                    .ToList();

                // Apply validation to rows 3-1000 (after header and labels rows)
                var range = sheet.Range($"{columnLetter}3:{columnLetter}1000");

                if (question.QuestionType == ServiceFormQuestionType.Checkbox)
                {
                    // For checkbox, add a comment about comma-separated values
                    var comment = sheet.Cell(1, i + 1).CreateComment();
                    comment.AddText($"Multi-select: Use comma-separated values.\nValid options: {string.Join(", ", optionValues)}");
                }
                else
                {
                    // For Radio/Select, use named range from Options sheet for validation
                    // This avoids the 255 character limit for inline validation
                    var namedRangeName = $"Options_{question.Code}";
                    var namedRange = workbook.DefinedNames.FirstOrDefault(nr => nr.Name == namedRangeName);
                    if (namedRange != null)
                    {
                        range.CreateDataValidation().List($"={namedRangeName}");
                    }
                }
            }

            // Add date format hint for date columns
            if (question.QuestionType == ServiceFormQuestionType.Date)
            {
                var dateComment = sheet.Cell(1, i + 1).CreateComment();
                dateComment.AddText("Format: YYYY-MM-DD");
            }

            // Mark required columns
            if (question.IsRequired)
            {
                sheet.Cell(1, i + 1).Style.Font.FontColor = XLColor.DarkRed;
            }
        }

        // Auto-fit columns
        sheet.Columns().AdjustToContents();
    }

    private static void CreateInstructionsSheet(IXLWorksheet sheet, List<ServiceFormQuestion> questions)
    {
        // Headers
        sheet.Cell(1, 1).Value = "Code";
        sheet.Cell(1, 2).Value = "Question";
        sheet.Cell(1, 3).Value = "Type";
        sheet.Cell(1, 4).Value = "Required";
        sheet.Cell(1, 5).Value = "Valid Options";
        sheet.Cell(1, 6).Value = "Hint";
        sheet.Cell(1, 7).Value = "Only applies when";

        sheet.Row(1).Style.Font.Bold = true;
        sheet.Row(1).Style.Fill.BackgroundColor = XLColor.LightGray;

        // Build a code→label lookup for resolving conditional parent labels and option labels
        var questionByCode = questions.ToDictionary(q => q.Code);

        // Data rows
        for (var i = 0; i < questions.Count; i++)
        {
            var question = questions[i];
            var row = i + 2;

            sheet.Cell(row, 1).Value = question.Code;
            sheet.Cell(row, 2).Value = question.Label;
            sheet.Cell(row, 3).Value = GetQuestionTypeName(question.QuestionType);
            sheet.Cell(row, 4).Value = question.IsRequired ? "Yes" : "No";

            if (question.Code == "SMD19")
            {
                // SMD19's options are populated at runtime from the organisation's registered delivery sites,
                // so the template can't list valid values. Spell out for the user exactly what to type.
                sheet.Cell(row, 5).Value = "Enter your organisation's registered delivery site names, exactly as they appear in your locations list. For multiple sites, separate with commas (e.g., \"Family Hub North, Family Hub South\").";
                sheet.Cell(row, 5).Style.Font.Italic = true;
                sheet.Cell(row, 5).Style.Alignment.WrapText = true;
            }
            else if (question.Options.Any())
            {
                var optionsList = question.Options
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => $"{o.Value} ({o.Label})")
                    .ToList();
                sheet.Cell(row, 5).Value = string.Join(", ", optionsList);
            }
            else if (!string.IsNullOrWhiteSpace(question.HelpText))
            {
                // Dynamically populated options — show the help text as guidance
                sheet.Cell(row, 5).Value = question.HelpText;
                sheet.Cell(row, 5).Style.Font.Italic = true;
            }

            sheet.Cell(row, 6).Value = question.Hint ?? string.Empty;

            // Conditional dependency — resolve parent code and option label(s) for readability
            if (!string.IsNullOrWhiteSpace(question.ConditionalQuestionCode) &&
                !string.IsNullOrWhiteSpace(question.ConditionalValue))
            {
                questionByCode.TryGetValue(question.ConditionalQuestionCode, out var parentQuestion);
                var parentLabel = parentQuestion?.Label ?? question.ConditionalQuestionCode;

                var conditionalValues = question.ConditionalValue.Split(',').Select(v => v.Trim());
                var conditionalLabels = conditionalValues.Select(cv =>
                {
                    var opt = parentQuestion?.Options.FirstOrDefault(o => o.Value == cv);
                    return opt?.Label ?? cv;
                });

                sheet.Cell(row, 7).Value = $"{parentLabel} = {string.Join(" or ", conditionalLabels)}";
            }

            if (question.IsRequired)
            {
                sheet.Cell(row, 4).Style.Font.FontColor = XLColor.DarkRed;
            }
        }

        sheet.Columns().AdjustToContents();
    }

    private static void CreateOptionsSheet(IXLWorksheet sheet, List<ServiceFormQuestion> questions, IXLWorkbook workbook)
    {
        var column = 1;
        foreach (var question in questions.Where(q => q.Options.Any() && q.QuestionType != ServiceFormQuestionType.Checkbox))
        {
            sheet.Cell(1, column).Value = question.Code;
            var row = 2;
            foreach (var option in question.Options.OrderBy(o => o.DisplayOrder))
            {
                sheet.Cell(row, column).Value = option.Value;
                row++;
            }

            // Create named range for this question's options (excluding header)
            var columnLetter = GetColumnLetter(column);
            var lastRow = row - 1;
            if (lastRow >= 2)
            {
                var rangeAddress = $"Options!${columnLetter}$2:${columnLetter}${lastRow}";
                workbook.DefinedNames.Add($"Options_{question.Code}", rangeAddress);
            }

            column++;
        }
    }

    private static string GetColumnLetter(int columnNumber)
    {
        var dividend = columnNumber;
        var columnName = string.Empty;

        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar(65 + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName;
    }

    private static string GetQuestionTypeName(ServiceFormQuestionType type)
    {
        return type switch
        {
            ServiceFormQuestionType.Text => "Text",
            ServiceFormQuestionType.Radio => "Single Select (Dropdown)",
            ServiceFormQuestionType.Checkbox => "Multi Select (Comma-separated)",
            ServiceFormQuestionType.Select => "Single Select (Dropdown)",
            ServiceFormQuestionType.Date => "Date (YYYY-MM-DD)",
            _ => "Unknown"
        };
    }
}