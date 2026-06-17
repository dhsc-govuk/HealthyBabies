using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Permissions;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using LanguageExt;
using MediatR;

namespace Application.Submissions.Commands;

public record ValidateBulkUploadCommand : IRequest<Either<string, BulkUploadValidationResultDto>>
{
    public Guid SubmissionId { get; init; }
    public Guid ModuleId { get; init; }
    public Stream FileStream { get; init; } = null!;
    public string FileName { get; init; } = string.Empty;
}

public class ValidateBulkUploadCommandHandler(
    PermissionsService permissionsService,
    IDataCollectionFormModuleQueries formModuleQueries,
    IServiceQueries serviceQueries,
    IBlobService blobService)
    : IRequestHandler<ValidateBulkUploadCommand, Either<string, BulkUploadValidationResultDto>>
{
    public async Task<Either<string, BulkUploadValidationResultDto>> Handle(
        ValidateBulkUploadCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();

        return await permissions.MatchAsync(
            async p => await ValidateUpload(p, request, cancellationToken),
            e => (Either<string, BulkUploadValidationResultDto>)e.Message);
    }

    private async Task<Either<string, BulkUploadValidationResultDto>> ValidateUpload(
        Permission permission,
        ValidateBulkUploadCommand request,
        CancellationToken cancellationToken)
    {
        var modId = new DataCollectionFormModuleId(request.ModuleId);
        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);

        if (formModuleOption.IsNone)
        {
            return "Form module not found";
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);
        var fields = GetOrderedFields(formModule);

        var orgId = permission.OrganisationId.Match(id => id, () => throw new InvalidOperationException("Organisation ID required"));
        var services = await serviceQueries.GetByOrganisationIdAsync(orgId, cancellationToken);
        var serviceNames = services.Select(s => s.Name.ToLowerInvariant()).ToHashSet();

        var buffer = new MemoryStream();
        await request.FileStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var rows = await BulkUploadCsvHelper.ParseCsvAsync(buffer, cancellationToken);

        if (rows.Count < 2)
        {
            return new BulkUploadValidationResultDto(
                false,
                0,
                0,
                0,
                new List<BulkUploadRowValidationDto>
                {
                    new(1, null, false, new List<BulkUploadFieldErrorDto>
                    {
                        new(string.Empty, string.Empty, "CSV file must have at least a header row and one data row")
                    })
                },
                new List<BulkUploadFieldMetadataDto>(),
                Guid.Empty);
        }

        var headers = rows[0];
        var dataRows = rows.Skip(1).ToList();
        var skippedRows = 1; // Start with 1 for header row

        // Skip label row if present
        if (dataRows.Count > 0 && BulkUploadCsvHelper.IsLabelRow(dataRows[0], fields))
        {
            dataRows = dataRows.Skip(1).ToList();
            skippedRows++;
        }

        // Skip required indicators row (R/O) if present
        if (dataRows.Count > 0 && BulkUploadCsvHelper.IsRequiredIndicatorsRow(dataRows[0]))
        {
            dataRows = dataRows.Skip(1).ToList();
            skippedRows++;
        }

        var rowValidations = new List<BulkUploadRowValidationDto>();
        var validCount = 0;
        var invalidCount = 0;

        for (var i = 0; i < dataRows.Count; i++)
        {
            var row = dataRows[i];
            // Row number is 1-indexed and accounts for skipped header/label rows
            var rowNumber = i + skippedRows + 1;
            var rowData = BulkUploadCsvHelper.MapRowToFields(headers, row);

            var serviceName = rowData.GetValueOrDefault("ServiceName") ?? rowData.GetValueOrDefault("PPS01");
            var errors = ValidateRow(rowData, fields, serviceNames, formModule.Code);

            var isValid = errors.Count == 0;
            if (isValid)
            {
                validCount++;
            }
            else
            {
                invalidCount++;
            }

            rowValidations.Add(new BulkUploadRowValidationDto(rowNumber, serviceName, isValid, errors));
        }

        // Build field metadata for fields with options (radio, select, checkbox)
        var fieldMetadata = BuildFieldMetadata(fields, services);

        var stagingId = Guid.NewGuid();
        buffer.Position = 0;
        var uploadOptions = new BlobWriteOptions(
            ContentType: "text/csv",
            Metadata: new Dictionary<string, string>
            {
                ["submissionId"] = request.SubmissionId.ToString(),
                ["moduleId"] = request.ModuleId.ToString(),
                ["uploadedAtUtc"] = DateTime.UtcNow.ToString("O"),
                ["originalFileName"] = request.FileName,
            });

        var streamResult = await blobService.OpenWriteStream(
            BulkUploadCsvHelper.BulkUploadStagingContainer,
            $"{stagingId}.csv",
            uploadOptions,
            cancellationToken);

        var stageOutcome = await streamResult.MatchAsync<Either<string, Guid>>(
            async writeStream =>
            {
                await using (writeStream)
                {
                    await buffer.CopyToAsync(writeStream, cancellationToken);
                }

                return stagingId;
            },
            ex => Either<string, Guid>.Left($"Failed to stage upload: {ex.Message}"));

        return stageOutcome.Match<Either<string, BulkUploadValidationResultDto>>(
            id => new BulkUploadValidationResultDto(
                invalidCount == 0,
                dataRows.Count,
                validCount,
                invalidCount,
                rowValidations,
                fieldMetadata,
                id),
            error => error);
    }

    private static List<BulkUploadFieldMetadataDto> BuildFieldMetadata(
        List<FormField> fields,
        IReadOnlyList<Domain.Services.Service> services)
    {
        var metadata = new List<BulkUploadFieldMetadataDto>();

        foreach (var field in fields)
        {
            var fieldType = field.FieldType.ToString();

            // Check if this field has dynamic options (e.g., PPS01 with dynamicOptions: "services")
            var hasDynamicServiceOptions = !string.IsNullOrEmpty(field.Configuration) &&
                                           field.Configuration.Contains("\"dynamicOptions\"") &&
                                           field.Configuration.Contains("\"services\"");

            List<BulkUploadFieldOptionDto> options;

            if (hasDynamicServiceOptions)
            {
                // Use service names as options for dynamic service fields
                options = services
                    .OrderBy(s => s.Name)
                    .Select(s => new BulkUploadFieldOptionDto(s.Name, s.Name))
                    .ToList();
            }
            else
            {
                // Use static options from field definition
                options = field.Options
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => new BulkUploadFieldOptionDto(o.Value, o.Label))
                    .ToList();
            }

            metadata.Add(new BulkUploadFieldMetadataDto(
                field.FieldKey,
                fieldType,
                field.IsRequired,
                options,
                field.ConditionalRules,
                field.Configuration));
        }

        return metadata;
    }

    private static List<FormField> GetOrderedFields(DataCollectionFormModule formModule)
    {
        return formModule.Fields
            .Where(f => f.IsActive)
            .OrderBy(f => f.DisplayOrder)
            .ToList();
    }

    private static List<BulkUploadFieldErrorDto> ValidateRow(
        Dictionary<string, string> rowData,
        List<FormField> fields,
        System.Collections.Generic.HashSet<string> serviceNames,
        string moduleCode)
    {
        var errors = new List<BulkUploadFieldErrorDto>();

        // For QSU module, validate ServiceName column
        // For PPS module, PPS01 is the service field and is validated as a regular field with dynamic options
        if (moduleCode.StartsWith("QSU", StringComparison.OrdinalIgnoreCase))
        {
            var serviceName = rowData.GetValueOrDefault("ServiceName");
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                errors.Add(new BulkUploadFieldErrorDto("ServiceName", "Service Name", "Service name is required"));
            }
            else if (!serviceNames.Contains(serviceName.ToLowerInvariant()))
            {
                errors.Add(new BulkUploadFieldErrorDto("ServiceName", "Service Name", $"Service '{serviceName}' not found"));
            }
        }

        foreach (var field in fields)
        {
            if (!IsFieldVisible(field, rowData, fields))
            {
                continue;
            }

            var value = rowData.GetValueOrDefault(field.FieldKey);
            var fieldErrors = ValidateFieldValue(value, field, serviceNames);

            foreach (var error in fieldErrors)
            {
                errors.Add(new BulkUploadFieldErrorDto(field.FieldKey, field.Label, error));
            }
        }

        return errors;
    }

    private static List<string> ValidateFieldValue(string? value, FormField field, System.Collections.Generic.HashSet<string> serviceNames)
    {
        var errors = new List<string>();

        if (field.IsRequired && string.IsNullOrWhiteSpace(value))
        {
            errors.Add("This field is required");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return errors;
        }

        var fieldTypeValue = field.FieldType.ToString().ToLowerInvariant();

        // Check if this is a dynamic options field (e.g., PPS01 with dynamicOptions: "services")
        var hasDynamicOptions = !string.IsNullOrEmpty(field.Configuration) &&
                                field.Configuration.Contains("dynamicOptions");

        if (fieldTypeValue == FieldTypeConstants.Number)
        {
            // Allow integers and decimals, trim whitespace
            var trimmedValue = value.Trim();
            if (!int.TryParse(trimmedValue, out _) && !decimal.TryParse(trimmedValue, out _))
            {
                errors.Add("Must be a valid number");
            }
        }
        else if (fieldTypeValue == FieldTypeConstants.Radio || fieldTypeValue == FieldTypeConstants.Select)
        {
            if (hasDynamicOptions && field.Configuration!.Contains("\"services\""))
            {
                // Validate against service names for dynamic service options
                var trimmedValue = value.Trim().ToLowerInvariant();
                if (!serviceNames.Contains(trimmedValue))
                {
                    errors.Add($"Service '{value}' not found");
                }
            }
            else if (field.Options.Any())
            {
                // Validate against static options
                var validOptions = field.Options.Select(o => o.Value.ToLowerInvariant()).ToHashSet();
                var validLabels = field.Options.Select(o => o.Label.ToLowerInvariant()).ToHashSet();

                var trimmedValue = value.Trim().ToLowerInvariant();
                if (!validOptions.Contains(trimmedValue) && !validLabels.Contains(trimmedValue))
                {
                    var optionList = string.Join(", ", field.Options.Select(o => o.Label));
                    errors.Add($"Invalid option. Valid options: {optionList}");
                }
            }
        }
        else if (fieldTypeValue == FieldTypeConstants.Checkbox)
        {
            // Only validate if field has options defined
            if (field.Options.Any())
            {
                var values = value.Split(',').Select(v => v.Trim().ToLowerInvariant()).Where(v => !string.IsNullOrEmpty(v)).ToList();
                var checkboxOptions = field.Options.Select(o => o.Value.ToLowerInvariant()).ToHashSet();
                var checkboxLabels = field.Options.Select(o => o.Label.ToLowerInvariant()).ToHashSet();

                var invalidValues = values.Where(v =>
                    !checkboxOptions.Contains(v) && !checkboxLabels.Contains(v)).ToList();

                if (invalidValues.Any())
                {
                    var optionList = string.Join(", ", field.Options.Select(o => o.Label));
                    errors.Add($"Invalid options: {string.Join(", ", invalidValues)}. Valid options: {optionList}");
                }
            }
        }

        return errors;
    }

    private static bool IsFieldVisible(FormField field, Dictionary<string, string> rowData, List<FormField> allFields)
        => ConditionalRuleEvaluator.IsFieldVisible(field, rowData, allFields);
}