using System.Text;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using ClosedXML.Excel;
using Domain.ServiceCategoryForms;
using Domain.Systems;

namespace Api.Services.Implementation;

public class ServiceCategoriesBulkUploadTemplateService(
    IServiceCategoryFormQuestionQueries serviceCategoryFormQuestionQueries,
    IServiceCategoryQueries serviceCategoryQueries,
    IGlobalDataQueries globalDataQueries) : IServiceCategoriesBulkUploadTemplateService
{
    public async Task<byte[]> GenerateCsvTemplateAsync(Guid organisationId, CancellationToken cancellationToken = default)
    {
        var questions = await serviceCategoryFormQuestionQueries.GetAll(cancellationToken);
        var orderedQuestions = questions
            .Where(q => q.IsActive)
            .OrderBy(q => q.Step)
            .ThenBy(q => q.DisplayOrder)
            .ToList();

        // Get all wider service categories
        var allCategories = await globalDataQueries.GetByEntityAsync(GlobalDataEntityTypes.WiderServiceCategory, cancellationToken);

        // Get existing service categories for this organisation
        var existingCategories = await serviceCategoryQueries.GetByOrganisationId(organisationId, cancellationToken);
        var existingCategoryCodes = existingCategories.Select(c => c.CategoryCode).ToHashSet();

        // Filter to categories not yet added
        var missingCategories = allCategories
            .Where(c => !existingCategoryCodes.Contains(c.Value))
            .OrderBy(c => c.Description)
            .ToList();

        var sb = new StringBuilder();

        // Row 1: Header row - "Category Name" + question codes
        var headers = new List<string> { "Category Name" };
        headers.AddRange(orderedQuestions.Select(q => EscapeCsvField(q.Code)));
        sb.AppendLine(string.Join(",", headers));

        // Row 2: Labels row - empty for category name column + question labels
        var labels = new List<string> { string.Empty };
        labels.AddRange(orderedQuestions.Select(q => EscapeCsvField(q.Label)));
        sb.AppendLine(string.Join(",", labels));

        // Row 3+: Pre-populate with missing categories
        foreach (var category in missingCategories)
        {
            var row = new List<string> { EscapeCsvField(category.Description ?? string.Empty) };
            row.AddRange(orderedQuestions.Select(_ => string.Empty));
            sb.AppendLine(string.Join(",", row));
        }

        // If no missing categories, add one empty row
        if (!missingCategories.Any())
        {
            var emptyRow = new List<string> { string.Empty };
            emptyRow.AddRange(orderedQuestions.Select(_ => string.Empty));
            sb.AppendLine(string.Join(",", emptyRow));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return field;
        }

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    public async Task<byte[]> GenerateExcelTemplateAsync(Guid organisationId, CancellationToken cancellationToken = default)
    {
        var questions = await serviceCategoryFormQuestionQueries.GetAll(cancellationToken);
        var orderedQuestions = questions
            .Where(q => q.IsActive)
            .OrderBy(q => q.Step)
            .ThenBy(q => q.DisplayOrder)
            .ToList();

        // Get all wider service categories
        var allCategories = await globalDataQueries.GetByEntityAsync(GlobalDataEntityTypes.WiderServiceCategory, cancellationToken);

        // Get existing service categories for this organisation
        var existingCategories = await serviceCategoryQueries.GetByOrganisationId(organisationId, cancellationToken);
        var existingCategoryCodes = existingCategories.Select(c => c.CategoryCode).ToHashSet();

        // Filter to categories not yet added
        var missingCategories = allCategories
            .Where(c => !existingCategoryCodes.Contains(c.Value))
            .OrderBy(c => c.Description)
            .ToList();

        using var workbook = new XLWorkbook();

        // Create hidden options sheet FIRST (needed for named ranges)
        var optionsSheet = workbook.Worksheets.Add("Options");
        CreateOptionsSheet(optionsSheet, orderedQuestions, missingCategories, workbook);
        optionsSheet.Hide();

        // Create main data sheet (uses named ranges from Options sheet)
        var dataSheet = workbook.Worksheets.Add("Data");
        CreateDataSheet(dataSheet, orderedQuestions, workbook, missingCategories);

        // Create instructions sheet
        var instructionsSheet = workbook.Worksheets.Add("Instructions");
        CreateInstructionsSheet(instructionsSheet, orderedQuestions);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void CreateDataSheet(IXLWorksheet sheet, List<ServiceCategoryFormQuestion> questions, IXLWorkbook workbook, List<GlobalData> missingCategories)
    {
        // Row 1: Header row - "Category Name" in column A, then question codes
        var categoryNameHeader = sheet.Cell(1, 1);
        categoryNameHeader.Value = "Category Name";
        categoryNameHeader.Style.Font.Bold = true;
        categoryNameHeader.Style.Fill.BackgroundColor = XLColor.LightGray;

        for (var i = 0; i < questions.Count; i++)
        {
            var cell = sheet.Cell(1, i + 2); // Offset by 1 for Category Name column
            cell.Value = questions[i].Code;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;

            if (questions[i].IsRequired)
            {
                cell.Style.Font.FontColor = XLColor.DarkRed;
            }
        }

        // Row 2: Labels row - empty for category name, then question labels
        for (var i = 0; i < questions.Count; i++)
        {
            sheet.Cell(2, i + 2).Value = questions[i].Label; // Offset by 1
        }

        // Row 3+: Add placeholder rows for missing categories
        if (missingCategories.Any())
        {
            for (var rowIndex = 0; rowIndex < missingCategories.Count; rowIndex++)
            {
                var dataRow = rowIndex + 3;

                // Add placeholder text in Category Name column (column A)
                var cell = sheet.Cell(dataRow, 1);
                cell.Value = "{widerServiceCategoryName}";
                cell.Style.Font.Italic = true;
                cell.Style.Font.FontColor = XLColor.Gray;
            }
        }
        else
        {
            // Add one empty placeholder row
            var cell = sheet.Cell(3, 1);
            cell.Value = "{widerServiceCategoryName}";
            cell.Style.Font.Italic = true;
            cell.Style.Font.FontColor = XLColor.Gray;
        }

        // Add data validation for Category Name column (column A)
        var categoryNameColumnLetter = GetColumnLetter(1);
        var categoryNameRange = sheet.Range($"{categoryNameColumnLetter}3:{categoryNameColumnLetter}1000");
        var categoryNameNamedRange = workbook.DefinedNames.FirstOrDefault(nr => nr.Name == "Options_CategoryName");
        if (categoryNameNamedRange != null)
        {
            categoryNameRange.CreateDataValidation().List("=Options_CategoryName");
        }

        // Add data validation for dropdown columns (offset by 1 for Category Name column)
        for (var i = 0; i < questions.Count; i++)
        {
            var question = questions[i];
            var columnLetter = GetColumnLetter(i + 2); // Offset by 1

            if (question.Options.Any())
            {
                var optionValues = question.Options
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => o.Value)
                    .ToList();

                // Apply validation to rows 3-1000 (after header and labels rows)
                var range = sheet.Range($"{columnLetter}3:{columnLetter}1000");

                if (question.QuestionType == ServiceCategoryFormQuestionType.Checkbox)
                {
                    // For checkbox, add a comment about comma-separated values
                    var comment = sheet.Cell(1, i + 2).CreateComment();
                    comment.AddText($"Multi-select: Use comma-separated values.\nValid options: {string.Join(", ", optionValues)}");
                }
                else
                {
                    // For Radio/Select, use named range from Options sheet for validation
                    var namedRangeName = $"Options_{question.Code}";
                    var namedRange = workbook.DefinedNames.FirstOrDefault(nr => nr.Name == namedRangeName);
                    if (namedRange != null)
                    {
                        range.CreateDataValidation().List($"={namedRangeName}");
                    }
                }
            }

            // Add date format hint for date columns
            if (question.QuestionType == ServiceCategoryFormQuestionType.Date)
            {
                var dateComment = sheet.Cell(1, i + 2).CreateComment();
                dateComment.AddText("Format: YYYY-MM-DD");
            }
        }

        // Auto-fit columns
        sheet.Columns().AdjustToContents();
    }

    private static void CreateInstructionsSheet(IXLWorksheet sheet, List<ServiceCategoryFormQuestion> questions)
    {
        // Headers
        sheet.Cell(1, 1).Value = "Code";
        sheet.Cell(1, 2).Value = "Question";
        sheet.Cell(1, 3).Value = "Type";
        sheet.Cell(1, 4).Value = "Required";
        sheet.Cell(1, 5).Value = "Valid Options";
        sheet.Cell(1, 6).Value = "Hint";

        sheet.Row(1).Style.Font.Bold = true;
        sheet.Row(1).Style.Fill.BackgroundColor = XLColor.LightGray;

        // First row: Category Name column info
        sheet.Cell(2, 1).Value = "Category Name";
        sheet.Cell(2, 2).Value = "The name of the wider service category";
        sheet.Cell(2, 3).Value = "Text";
        sheet.Cell(2, 4).Value = "Yes";
        sheet.Cell(2, 4).Style.Font.FontColor = XLColor.DarkRed;
        sheet.Cell(2, 5).Value = "Select from available wider service categories";
        sheet.Cell(2, 6).Value = string.Empty;

        // Data rows for questions
        for (var i = 0; i < questions.Count; i++)
        {
            var question = questions[i];
            var row = i + 3; // Offset by 2 (header + Category Name row)

            sheet.Cell(row, 1).Value = question.Code;
            sheet.Cell(row, 2).Value = question.Label;
            sheet.Cell(row, 3).Value = GetQuestionTypeName(question.QuestionType);
            sheet.Cell(row, 4).Value = question.IsRequired ? "Yes" : "No";

            if (question.Options.Any())
            {
                var optionsList = question.Options
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => $"{o.Value} ({o.Label})")
                    .ToList();
                sheet.Cell(row, 5).Value = string.Join(", ", optionsList);
            }

            sheet.Cell(row, 6).Value = question.Hint ?? string.Empty;

            if (question.IsRequired)
            {
                sheet.Cell(row, 4).Style.Font.FontColor = XLColor.DarkRed;
            }
        }

        sheet.Columns().AdjustToContents();
    }

    private static void CreateOptionsSheet(IXLWorksheet sheet, List<ServiceCategoryFormQuestion> questions, List<GlobalData> missingCategories, IXLWorkbook workbook)
    {
        var column = 1;

        // Add Category Name options (wider service categories)
        if (missingCategories.Any())
        {
            sheet.Cell(1, column).Value = "CategoryName";
            var row = 2;
            foreach (var category in missingCategories.OrderBy(c => c.Description))
            {
                sheet.Cell(row, column).Value = category.Description ?? string.Empty;
                row++;
            }

            var columnLetter = GetColumnLetter(column);
            var lastRow = row - 1;
            if (lastRow >= 2)
            {
                var rangeAddress = $"Options!${columnLetter}$2:${columnLetter}${lastRow}";
                workbook.DefinedNames.Add("Options_CategoryName", rangeAddress);
            }

            column++;
        }

        // Add question options
        foreach (var question in questions.Where(q => q.Options.Any() && q.QuestionType != ServiceCategoryFormQuestionType.Checkbox))
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

    private static string GetQuestionTypeName(ServiceCategoryFormQuestionType type)
    {
        return type switch
        {
            ServiceCategoryFormQuestionType.Text => "Text",
            ServiceCategoryFormQuestionType.Radio => "Single Select (Dropdown)",
            ServiceCategoryFormQuestionType.Checkbox => "Multi Select (Comma-separated)",
            ServiceCategoryFormQuestionType.Select => "Single Select (Dropdown)",
            ServiceCategoryFormQuestionType.Date => "Date (YYYY-MM-DD)",
            _ => "Unknown"
        };
    }
}