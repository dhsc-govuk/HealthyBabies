using System.Text;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using ClosedXML.Excel;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;
using Domain.Services;

namespace Api.Services.Implementation;

public class SubmissionsBulkUploadService(
    IDataCollectionFormModuleQueries formModuleQueries,
    IServiceQueries serviceQueries,
    IFormSubmissionRepository formSubmissionRepository) : ISubmissionsBulkUploadService
{
    public async Task<byte[]> GenerateCsvTemplateAsync(
        Guid organisationId,
        Guid moduleId,
        Guid submissionId,
        Guid dataCollectionId,
        CancellationToken cancellationToken = default)
    {
        var modId = new DataCollectionFormModuleId(moduleId);
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);
        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);

        if (formModuleOption.IsNone)
        {
            return Array.Empty<byte>();
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);
        var fields = GetOrderedFields(formModule);
        var isOutcomeScores = formModule.Code == DataCollectionFormModuleCodes.OutcomeScores;

        // Get services for this organisation based on module type
        List<Service> incompleteServices;
        if (isOutcomeScores)
        {
            // For Outcome Scores, only include services where QSU13 = "yes"
            incompleteServices = await GetServicesRequiringOutcomeScoresAsync(orgId, modId, dcId, cancellationToken);
        }
        else
        {
            incompleteServices = await GetIncompleteServicesAsync(orgId, modId, dcId, cancellationToken);
        }

        var sb = new StringBuilder();

        // Row 1: Header row with field codes
        var headers = new List<string>();
        if (!isOutcomeScores)
        {
            // For non-Outcome Scores modules, add ServiceName column
            // Outcome Scores uses PPS01 as the service field, AnonymisedId is auto-generated on upload
            headers.Add("ServiceName");
        }

        headers.AddRange(fields.Select(f => EscapeCsvField(f.FieldKey)));
        sb.AppendLine(string.Join(",", headers));

        // Row 2: Labels row
        var labels = new List<string>();
        if (!isOutcomeScores)
        {
            labels.Add("Service Name");
        }

        labels.AddRange(fields.Select(f => EscapeCsvField(f.Label)));
        sb.AppendLine(string.Join(",", labels));

        // Row 3+: Pre-fill rows with incomplete service names (empty values for user to fill)
        foreach (var service in incompleteServices)
        {
            var row = new List<string>();
            if (isOutcomeScores)
            {
                // For Outcome Scores, pre-fill PPS01 with service name, leave other fields empty
                // AnonymisedId is auto-generated on upload, not needed in template
                row.AddRange(fields.Select(f => f.FieldKey == "PPS01" ? EscapeCsvField(service.Name) : string.Empty));
            }
            else
            {
                // For other modules, add ServiceName and leave all fields empty
                row.Add(EscapeCsvField(service.Name));
                row.AddRange(fields.Select(_ => string.Empty));
            }

            sb.AppendLine(string.Join(",", row));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> GenerateExcelTemplateAsync(
        Guid organisationId,
        Guid moduleId,
        Guid submissionId,
        Guid dataCollectionId,
        CancellationToken cancellationToken = default)
    {
        var modId = new DataCollectionFormModuleId(moduleId);
        var orgId = new OrganisationId(organisationId);
        var dcId = new DataCollectionId(dataCollectionId);
        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);

        if (formModuleOption.IsNone)
        {
            return Array.Empty<byte>();
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);
        var fields = GetOrderedFields(formModule);
        var isOutcomeScores = formModule.Code == DataCollectionFormModuleCodes.OutcomeScores;

        // Get services for this organisation based on module type
        List<Service> incompleteServices;
        if (isOutcomeScores)
        {
            // For Outcome Scores, only include services where QSU13 = "yes"
            incompleteServices = await GetServicesRequiringOutcomeScoresAsync(orgId, modId, dcId, cancellationToken);
        }
        else
        {
            incompleteServices = await GetIncompleteServicesAsync(orgId, modId, dcId, cancellationToken);
        }

        using var workbook = new XLWorkbook();

        // Create hidden options sheet FIRST (needed for named ranges)
        var optionsSheet = workbook.Worksheets.Add("Options");
        CreateOptionsSheet(optionsSheet, fields, incompleteServices, isOutcomeScores, workbook);
        optionsSheet.Hide();

        // Create main data sheet (uses named ranges from Options sheet)
        var dataSheet = workbook.Worksheets.Add("Data");
        CreateDataSheet(dataSheet, fields, incompleteServices, isOutcomeScores, workbook);

        // Create instructions sheet
        var instructionsSheet = workbook.Worksheets.Add("Instructions");
        CreateInstructionsSheet(instructionsSheet, fields, isOutcomeScores);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static List<FormField> GetOrderedFields(DataCollectionFormModule formModule)
    {
        // Build a lookup of section IDs to section numbers
        var sectionLookup = formModule.Sections
            .ToDictionary(s => s.Id, s => s.SectionNumber);

        return formModule.Fields
            .Where(f => f.IsActive)
            .OrderBy(f => f.FormSectionId != null && sectionLookup.TryGetValue(f.FormSectionId, out var sectionNum) ? sectionNum : 0)
            .ThenBy(f => f.DisplayOrder)
            .ToList();
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

    private async Task<List<Service>> GetIncompleteServicesAsync(
        OrganisationId organisationId,
        DataCollectionFormModuleId moduleId,
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken)
    {
        // Get all services for this organisation
        var allServices = await serviceQueries.GetByOrganisationIdAsync(organisationId, cancellationToken);

        // Get all form submissions for this module filtered by entity type "Service" and data collection
        var serviceSubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            moduleId, organisationId, dataCollectionId, "Service", cancellationToken);

        // Get service IDs that have completed submissions (status = "approved" means Completed)
        var completedServiceIds = serviceSubmissions
            .Where(s => s.Status.Value == "approved")
            .Where(s => s.EntityId.HasValue)
            .Select(s => s.EntityId!.Value)
            .ToHashSet();

        // Return services that don't have completed submissions
        return allServices
            .Where(s => !completedServiceIds.Contains(s.Id.Value))
            .OrderBy(s => s.Name)
            .ToList();
    }

    private async Task<List<Service>> GetServicesRequiringOutcomeScoresAsync(
        OrganisationId organisationId,
        DataCollectionFormModuleId outcomeScoresModuleId,
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken)
    {
        // Get all services for this organisation
        var allServices = await serviceQueries.GetByOrganisationIdAsync(organisationId, cancellationToken);

        // Get the Service Users module to find the QSU13 field
        var serviceUsersModuleOption = await formModuleQueries.GetByCodeWithFieldsAsync(
            DataCollectionFormModuleCodes.ServiceUsers, cancellationToken);

        if (serviceUsersModuleOption.IsNone)
        {
            return new List<Service>();
        }

        var serviceUsersModule = serviceUsersModuleOption.Match(m => m, () => null!);

        // Find the QSU13 field (outcome scores collected question)
        var qsu13Field = serviceUsersModule.Fields.FirstOrDefault(f => f.FieldKey == "QSU13");
        if (qsu13Field == null)
        {
            return new List<Service>();
        }

        // Get all Service Users form submissions for this organisation and data collection
        var serviceSubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            serviceUsersModule.Id, organisationId, dataCollectionId, "Service", cancellationToken);

        // Get service IDs where QSU13 = "yes"
        var servicesRequiringOutcomeScores = new HashSet<Guid>();
        foreach (var submission in serviceSubmissions)
        {
            var qsu13Value = submission.FieldValues.FirstOrDefault(fv => fv.FormFieldId == qsu13Field.Id);
            if (qsu13Value?.Value == "yes" && submission.EntityId.HasValue)
            {
                servicesRequiringOutcomeScores.Add(submission.EntityId.Value);
            }
        }

        // Get completed outcome score submissions to exclude already completed services
        var outcomeScoreSubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            outcomeScoresModuleId, organisationId, dataCollectionId, "OutcomeScore", cancellationToken);

        var completedServiceIds = outcomeScoreSubmissions
            .Where(s => s.Status.Value == "approved")
            .Where(s => s.EntityId.HasValue && s.EntityId.Value != Guid.Empty)
            .Select(s => s.EntityId!.Value)
            .ToHashSet();

        // Return services that require outcome scores and don't have completed submissions
        return allServices
            .Where(s => servicesRequiringOutcomeScores.Contains(s.Id.Value))
            .Where(s => !completedServiceIds.Contains(s.Id.Value))
            .OrderBy(s => s.Name)
            .ToList();
    }

    private static void CreateOptionsSheet(IXLWorksheet sheet, List<FormField> fields, List<Service> services, bool isOutcomeScores, IXLWorkbook workbook)
    {
        var column = 1;

        // Add ServiceName options for non-Outcome Scores modules
        if (!isOutcomeScores && services.Any())
        {
            sheet.Cell(1, column).Value = "ServiceName";
            var row = 2;
            foreach (var service in services.OrderBy(s => s.Name))
            {
                sheet.Cell(row, column).Value = service.Name;
                row++;
            }

            var columnLetter = GetColumnLetter(column);
            var lastRow = row - 1;
            if (lastRow >= 2)
            {
                var rangeAddress = $"Options!${columnLetter}$2:${columnLetter}${lastRow}";
                workbook.DefinedNames.Add("Options_ServiceName", rangeAddress);
            }

            column++;
        }

        // Add PPS01 (service name) options for Outcome Scores module
        if (isOutcomeScores && services.Any())
        {
            sheet.Cell(1, column).Value = "PPS01";
            var row = 2;
            foreach (var service in services.OrderBy(s => s.Name))
            {
                sheet.Cell(row, column).Value = service.Name;
                row++;
            }

            var columnLetter = GetColumnLetter(column);
            var lastRow = row - 1;
            if (lastRow >= 2)
            {
                var rangeAddress = $"Options!${columnLetter}$2:${columnLetter}${lastRow}";
                workbook.DefinedNames.Add("Options_PPS01", rangeAddress);
            }

            column++;
        }

        // Add field options (excluding checkbox and multi_select which use comma-separated values)
        foreach (var field in fields.Where(f => f.Options.Any(o => o.IsActive) && f.FieldType.Value != "checkbox" && f.FieldType.Value != "multi_select"))
        {
            sheet.Cell(1, column).Value = field.FieldKey;
            var row = 2;
            foreach (var option in field.Options.Where(o => o.IsActive).OrderBy(o => o.DisplayOrder))
            {
                sheet.Cell(row, column).Value = option.Value;
                row++;
            }

            // Create named range for this field's options (excluding header)
            var columnLetter = GetColumnLetter(column);
            var lastRow = row - 1;
            if (lastRow >= 2)
            {
                var rangeAddress = $"Options!${columnLetter}$2:${columnLetter}${lastRow}";
                workbook.DefinedNames.Add($"Options_{field.FieldKey}", rangeAddress);
            }

            column++;
        }
    }

    private static void CreateDataSheet(IXLWorksheet sheet, List<FormField> fields, List<Service> incompleteServices, bool isOutcomeScores, IXLWorkbook workbook)
    {
        // Calculate column indices
        // For Outcome Scores: fields only (PPS01 is the service field, AnonymisedId is auto-generated on upload)
        // For other modules: ServiceName + fields
        var colIndex = 1;
        var serviceNameCol = isOutcomeScores ? -1 : colIndex++;
        var fieldStartCol = colIndex;

        // Header row with field codes
        if (!isOutcomeScores)
        {
            var serviceNameCell = sheet.Cell(1, serviceNameCol);
            serviceNameCell.Value = "ServiceName";
            serviceNameCell.Style.Font.Bold = true;
            serviceNameCell.Style.Fill.BackgroundColor = XLColor.LightGray;
            serviceNameCell.Style.Font.FontColor = XLColor.DarkRed; // Required
        }

        for (var i = 0; i < fields.Count; i++)
        {
            var cell = sheet.Cell(1, fieldStartCol + i);
            cell.Value = fields[i].FieldKey;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            if (fields[i].IsRequired)
            {
                cell.Style.Font.FontColor = XLColor.DarkRed;
            }
        }

        // Row 2: Labels row with field labels (questions)
        if (!isOutcomeScores)
        {
            sheet.Cell(2, serviceNameCol).Value = "Service Name";
        }

        for (var i = 0; i < fields.Count; i++)
        {
            sheet.Cell(2, fieldStartCol + i).Value = fields[i].Label;
        }

        // Pre-fill rows with incomplete service names
        var dataRowStart = 3;
        foreach (var service in incompleteServices)
        {
            if (isOutcomeScores)
            {
                // Pre-fill PPS01 with service name (AnonymisedId is auto-generated on upload)
                var pps01Index = fields.FindIndex(f => f.FieldKey == "PPS01");
                if (pps01Index >= 0)
                {
                    sheet.Cell(dataRowStart, fieldStartCol + pps01Index).Value = service.Name;
                }
            }
            else
            {
                sheet.Cell(dataRowStart, serviceNameCol).Value = service.Name;
            }

            dataRowStart++;
        }

        // Add data validation for ServiceName column (non-Outcome Scores only)
        if (!isOutcomeScores)
        {
            var serviceNameColumnLetter = GetColumnLetter(serviceNameCol);
            var serviceNameRange = sheet.Range($"{serviceNameColumnLetter}3:{serviceNameColumnLetter}1000");
            var serviceNameNamedRange = workbook.DefinedNames.FirstOrDefault(nr => nr.Name == "Options_ServiceName");
            if (serviceNameNamedRange != null)
            {
                serviceNameRange.CreateDataValidation().List("=Options_ServiceName");
            }
        }

        // Add data validation for dropdown columns
        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var columnIndex = fieldStartCol + i;
            var columnLetter = GetColumnLetter(columnIndex);

            if (field.Options.Any(o => o.IsActive))
            {
                var optionValues = field.Options
                    .Where(o => o.IsActive)
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => o.Value)
                    .ToList();

                // Apply validation to rows 3-1000 (after header and labels rows)
                var range = sheet.Range($"{columnLetter}3:{columnLetter}1000");

                if (field.FieldType.Value == "checkbox" || field.FieldType.Value == "multi_select")
                {
                    // For checkbox/multi_select, add a comment about comma-separated values
                    var comment = sheet.Cell(1, columnIndex).CreateComment();
                    comment.AddText($"Multi-select: Use comma-separated values.\nValid options: {string.Join(", ", optionValues)}");
                }
                else
                {
                    // For all other field types with options (radio, select, etc.), use named range from Options sheet for validation
                    var namedRangeName = $"Options_{field.FieldKey}";
                    var namedRange = workbook.DefinedNames.FirstOrDefault(nr => nr.Name == namedRangeName);
                    if (namedRange != null)
                    {
                        range.CreateDataValidation().List($"={namedRangeName}");
                    }
                }
            }

            // Add PPS01 dropdown for Outcome Scores module
            if (isOutcomeScores && field.FieldKey == "PPS01")
            {
                var range = sheet.Range($"{columnLetter}3:{columnLetter}1000");
                var pps01NamedRange = workbook.DefinedNames.FirstOrDefault(nr => nr.Name == "Options_PPS01");
                if (pps01NamedRange != null)
                {
                    range.CreateDataValidation().List("=Options_PPS01");
                }
            }

            // Add date format hint for date columns
            if (field.FieldType.Value == "date")
            {
                var dateComment = sheet.Cell(1, columnIndex).CreateComment();
                dateComment.AddText("Format: DD/MM/YYYY");
            }
        }

        // Auto-fit columns
        sheet.Columns().AdjustToContents();
    }

    private static void CreateInstructionsSheet(IXLWorksheet sheet, List<FormField> fields, bool isOutcomeScores)
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

        var currentRow = 2;

        // For non-Outcome Scores modules, add ServiceName row
        // For Outcome Scores, PPS01 is the service field and AnonymisedId is auto-generated on upload
        if (!isOutcomeScores)
        {
            sheet.Cell(currentRow, 1).Value = "ServiceName";
            sheet.Cell(currentRow, 2).Value = "Service Name";
            sheet.Cell(currentRow, 3).Value = "Text";
            sheet.Cell(currentRow, 4).Value = "Yes";
            sheet.Cell(currentRow, 5).Value = "Must match a service in your organisation";
            sheet.Cell(currentRow, 6).Value = "Enter the exact name of the service as it appears in your service list";
            sheet.Cell(currentRow, 4).Style.Font.FontColor = XLColor.DarkRed;
            currentRow++;
        }

        // Field rows
        foreach (var field in fields)
        {
            sheet.Cell(currentRow, 1).Value = field.FieldKey;
            sheet.Cell(currentRow, 2).Value = field.Label;
            sheet.Cell(currentRow, 3).Value = GetFieldTypeName(field.FieldType);
            sheet.Cell(currentRow, 4).Value = field.IsRequired ? "Yes" : "No";

            if (field.Options.Any(o => o.IsActive))
            {
                var optionsList = field.Options
                    .Where(o => o.IsActive)
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => $"{o.Value} ({o.Label})")
                    .ToList();
                sheet.Cell(currentRow, 5).Value = string.Join(", ", optionsList);
            }

            sheet.Cell(currentRow, 6).Value = field.HelpText ?? string.Empty;

            if (field.IsRequired)
            {
                sheet.Cell(currentRow, 4).Style.Font.FontColor = XLColor.DarkRed;
            }

            currentRow++;
        }

        sheet.Columns().AdjustToContents();
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

    private static string GetFieldTypeName(FieldType type)
    {
        return type.Value switch
        {
            "text" => "Text",
            "radio" => "Single Select (Dropdown)",
            "checkbox" => "Multi Select (Comma-separated)",
            "select" => "Single Select (Dropdown)",
            "date" => "Date (DD/MM/YYYY)",
            "number" => "Number",
            "textarea" => "Text (Long)",
            "email" => "Email",
            "phone" => "Phone",
            "url" => "URL",
            "file" => "File Upload",
            "address" => "Address",
            "post_code" => "Post Code",
            "multi_select" => "Multi Select (Comma-separated)",
            _ => type.Value
        };
    }
}