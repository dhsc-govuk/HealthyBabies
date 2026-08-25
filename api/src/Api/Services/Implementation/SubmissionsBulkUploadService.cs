using System.Text;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Submissions.Helpers;
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
    // Column widths in NoC (character widths). Excel's own default is 8.43; anything past roughly a
    // screen's worth is easier to read wrapped than scrolled.
    private const double MinColumnWidth = 10;
    private const double MaxColumnWidth = 60;

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
        var isActivityModule = formModule.AllowsMultipleActivities;
        var fields = BulkUploadTemplateColumns.ForModule(formModule);
        var isOutcomeScores = formModule.Code == DataCollectionFormModuleCodes.OutcomeScores;
        var hasServiceNameColumn = !isOutcomeScores && !isActivityModule;

        // Get services for this organisation based on module type. Activity modules (Delivery
        // Plan strands) create a brand-new record per row rather than matching an existing
        // Service, so there's no fixed set of rows to pre-fill.
        List<Service> incompleteServices;
        if (isActivityModule)
        {
            incompleteServices = new List<Service>();
        }
        else if (isOutcomeScores)
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
        if (hasServiceNameColumn)
        {
            // For non-Outcome Scores, non-Activity modules, add ServiceName column
            // Outcome Scores uses PPS01 as the service field, AnonymisedId is auto-generated on upload
            headers.Add("ServiceName");
        }

        headers.AddRange(fields.Select(f => EscapeCsvField(f.FieldKey)));
        sb.AppendLine(string.Join(",", headers));

        // Row 2: Labels row
        var labels = new List<string>();
        if (hasServiceNameColumn)
        {
            labels.Add("Service Name");
        }

        labels.AddRange(fields.Select(f => EscapeCsvField(f.Label.Replace("{serviceName}", "this service", StringComparison.OrdinalIgnoreCase))));
        sb.AppendLine(string.Join(",", labels));

        // Row 3: Valid options row, so form-builder option changes are visible in the CSV without
        // opening the Excel template. Upload skips it (BulkUploadCsvHelper.CountPreambleRows).
        // Omitted entirely when no field has options: an all-blank ",,,," row isn't whitespace-only,
        // so the parser would keep it and validate it as a bogus data row.
        if (HasOptionsRow(fields))
        {
            var optionCells = new List<string>();
            if (hasServiceNameColumn)
            {
                optionCells.Add(string.Empty);
            }

            optionCells.AddRange(fields.Select(f => EscapeCsvField(BulkUploadOptionsRow.BuildCell(f))));
            sb.AppendLine(string.Join(",", optionCells));
        }

        // Row 4+: Pre-fill rows with incomplete service names (empty values for user to fill)
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
        var isActivityModule = formModule.AllowsMultipleActivities;
        var fields = BulkUploadTemplateColumns.ForModule(formModule);
        var isOutcomeScores = formModule.Code == DataCollectionFormModuleCodes.OutcomeScores;
        var hasServiceNameColumn = !isOutcomeScores && !isActivityModule;

        // Get services for this organisation based on module type. Activity modules (Delivery
        // Plan strands) create a brand-new record per row rather than matching an existing
        // Service, so there's no fixed set of rows to pre-fill.
        List<Service> incompleteServices;
        if (isActivityModule)
        {
            incompleteServices = new List<Service>();
        }
        else if (isOutcomeScores)
        {
            // For Outcome Scores, only include services where QSU13 = "yes"
            incompleteServices = await GetServicesRequiringOutcomeScoresAsync(orgId, modId, dcId, cancellationToken);
        }
        else
        {
            incompleteServices = await GetIncompleteServicesAsync(orgId, modId, dcId, cancellationToken);
        }

        // "Rows to pre-fill" and "options for a dropdown" are different questions. Activity modules
        // have no rows to pre-fill, but their service-backed fields still need the full service list.
        var serviceOptions = isActivityModule && fields.Any(DynamicServiceOptions.IsServiceBacked)
            ? (await serviceQueries.GetByOrganisationIdAsync(orgId, cancellationToken)).ToList()
            : incompleteServices;

        using var workbook = new XLWorkbook();

        // Create hidden options sheet FIRST (needed for named ranges)
        var optionsSheet = workbook.Worksheets.Add("Options");
        CreateOptionsSheet(optionsSheet, fields, serviceOptions, isOutcomeScores, hasServiceNameColumn, workbook);
        optionsSheet.Hide();

        // Create main data sheet (uses named ranges from Options sheet)
        var dataSheet = workbook.Worksheets.Add("Data");
        CreateDataSheet(dataSheet, fields, incompleteServices, isOutcomeScores, hasServiceNameColumn, workbook);

        // Create instructions sheet
        var instructionsSheet = workbook.Worksheets.Add("Instructions");
        CreateInstructionsSheet(instructionsSheet, fields, hasServiceNameColumn);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // A row of blank cells would be parsed as data rather than skipped, so the options row is only
    // worth emitting when at least one column actually has something to list.
    private static bool HasOptionsRow(List<FormField> fields) =>
        fields.Any(f => BulkUploadOptionsRow.BuildCell(f).Length > 0);

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

    private static void CreateOptionsSheet(IXLWorksheet sheet, List<FormField> fields, List<Service> services, bool isOutcomeScores, bool hasServiceNameColumn, IXLWorkbook workbook)
    {
        var column = 1;

        // Add ServiceName options for non-Outcome Scores, non-Activity modules
        if (hasServiceNameColumn && services.Any())
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

        // Service-name options for every field configured with {"dynamicOptions":"services"} —
        // Outcome Scores' PPS01 and the delivery plan's "Which service is the activity funding?".
        foreach (var serviceField in fields.Where(DynamicServiceOptions.IsServiceBacked))
        {
            if (!services.Any())
            {
                break;
            }

            sheet.Cell(1, column).Value = serviceField.FieldKey;
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
                workbook.DefinedNames.Add($"Options_{SanitiseNamedRange(serviceField.FieldKey)}", rangeAddress);
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

    private static void CreateDataSheet(IXLWorksheet sheet, List<FormField> fields, List<Service> incompleteServices, bool isOutcomeScores, bool hasServiceNameColumn, IXLWorkbook workbook)
    {
        // Calculate column indices
        // For Outcome Scores / Activity modules: fields only (Outcome Scores uses PPS01 as the
        // service field; Activity modules have no service to key off at all)
        // For other modules: ServiceName + fields
        var colIndex = 1;
        var serviceNameCol = hasServiceNameColumn ? colIndex++ : -1;
        var fieldStartCol = colIndex;

        // Header row with field codes
        if (hasServiceNameColumn)
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
        if (hasServiceNameColumn)
        {
            sheet.Cell(2, serviceNameCol).Value = "Service Name";
        }

        for (var i = 0; i < fields.Count; i++)
        {
            sheet.Cell(2, fieldStartCol + i).Value = fields[i].Label;
        }

        // Row 3: the same valid-options row the CSV template carries. Uploads only accept .csv, so
        // this sheet reaches the parser via "Save As CSV" — it has to match the CSV row-for-row.
        // It's also the only visible place checkbox/multi-select options appear: CreateOptionsSheet
        // skips those field types, leaving just a cell comment most users never open.
        var hasOptionsRow = HasOptionsRow(fields);
        if (hasOptionsRow)
        {
            for (var i = 0; i < fields.Count; i++)
            {
                sheet.Cell(3, fieldStartCol + i).Value = BulkUploadOptionsRow.BuildCell(fields[i]);
            }

            sheet.Row(3).Style.Font.Italic = true;
            sheet.Row(3).Style.Font.FontColor = XLColor.Gray;
        }

        // Pre-fill rows with incomplete service names
        var firstDataRow = hasOptionsRow ? 4 : 3;
        var dataRowStart = firstDataRow;
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

        // Add data validation for ServiceName column (non-Outcome Scores, non-Activity only)
        if (hasServiceNameColumn)
        {
            var serviceNameColumnLetter = GetColumnLetter(serviceNameCol);
            var serviceNameRange = sheet.Range($"{serviceNameColumnLetter}{firstDataRow}:{serviceNameColumnLetter}1000");
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

                // Apply validation to the data rows only (after the header, labels and options rows)
                var range = sheet.Range($"{columnLetter}{firstDataRow}:{columnLetter}1000");

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

            // Dropdown for any service-backed field. Multi-value fields (checkboxes) still get the
            // list so users can see valid names, though Excel only enforces one value per cell.
            if (DynamicServiceOptions.IsServiceBacked(field))
            {
                var rangeName = $"Options_{SanitiseNamedRange(field.FieldKey)}";
                var range = sheet.Range($"{columnLetter}{firstDataRow}:{columnLetter}1000");
                if (workbook.DefinedNames.Any(nr => nr.Name == rangeName))
                {
                    range.CreateDataValidation().List($"={rangeName}");
                }
            }

            // Add date format hint for date columns
            if (field.FieldType.Value == "date")
            {
                var dateComment = sheet.Cell(1, columnIndex).CreateComment();
                dateComment.AddText("Format: DD/MM/YYYY");
            }
        }

        // Auto-fit columns, but cap the width: a question with many options makes the row 3 cell far
        // longer than its label, and an uncapped fit would leave the sheet unusably wide to scroll.
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        foreach (var column in sheet.ColumnsUsed())
        {
            column.AdjustToContents(1, lastRow, MinColumnWidth, MaxColumnWidth);
        }
    }

    private static void CreateInstructionsSheet(IXLWorksheet sheet, List<FormField> fields, bool hasServiceNameColumn)
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

        // For non-Outcome Scores, non-Activity modules, add ServiceName row
        // For Outcome Scores, PPS01 is the service field and AnonymisedId is auto-generated on upload
        if (hasServiceNameColumn)
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

    // Excel named ranges allow letters, digits and underscores only — delivery-plan service
    // fields are coded with a dot (DG05.2), so it has to be substituted.
    private static string SanitiseNamedRange(string fieldKey) => fieldKey.Replace(".", "_");

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