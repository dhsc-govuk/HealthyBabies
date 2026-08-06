using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;
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
    IFormSubmissionRepository formSubmissionRepository,
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
        var dcId = new DataCollectionId(request.SubmissionId);
        var allServices = await serviceQueries.GetByOrganisationIdAsync(orgId, cancellationToken);

        // For outcome-scores modules, only allow services that have QSU13 = "yes" in the Service Users
        // module — these are the only services actually collecting pre/post outcome scores.
        var services = formModule.Code == DataCollectionFormModuleCodes.OutcomeScores
            ? await FilterToEligibleServicesAsync(allServices, orgId, dcId, cancellationToken)
            : allServices;

        var serviceNames = services.Select(s => s.Name.ToLowerInvariant()).ToHashSet();

        // Service names aren't unique in the DB, so group rather than ToDictionary (which throws on a duplicate
        // key) — when two services share a name, resolve the ambiguity deterministically by taking the first.
        var serviceAnswersByName = services
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.First().Answers.ToDictionary(a => a.QuestionCode, a => a.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

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

        // Reject CSVs whose headers have no overlap with this module's field keys.
        // This catches the case where a user uploads a Service Users CSV on the Outcome Scores
        // upload page (or vice versa): all row errors would reference field codes that don't exist
        // in the CSV headers, so the frontend drops them silently and lets the user proceed.
        var expectedKeys = fields.Select(f => f.FieldKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (formModule.Code.StartsWith("QSU", StringComparison.OrdinalIgnoreCase))
        {
            expectedKeys.Add("ServiceName");
        }

        if (!headers.Any(h => expectedKeys.Contains(h)))
        {
            return new BulkUploadValidationResultDto(
                IsValid: false,
                TotalRows: 0,
                ValidRows: 0,
                InvalidRows: 0,
                RowValidations: Array.Empty<BulkUploadRowValidationDto>(),
                FieldMetadata: Array.Empty<BulkUploadFieldMetadataDto>(),
                StagingId: Guid.Empty,
                ModuleError: "This file does not match the expected template for this module. Please download the correct template and try again.");
        }

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
            var serviceAnswers = serviceName != null ? serviceAnswersByName.GetValueOrDefault(serviceName.ToLowerInvariant()) : null;
            var errors = ValidateRow(rowData, fields, serviceNames, formModule.Code, serviceAnswers);

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
        var fieldMetadata = BuildFieldMetadata(fields, services, formModule.Code);

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

    private async Task<IReadOnlyList<Domain.Services.Service>> FilterToEligibleServicesAsync(
        IReadOnlyList<Domain.Services.Service> allServices,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken)
    {
        var serviceUsersModuleOption = await formModuleQueries.GetByCodeWithFieldsAsync(
            DataCollectionFormModuleCodes.ServiceUsers, cancellationToken);

        if (serviceUsersModuleOption.IsNone)
        {
            return Array.Empty<Domain.Services.Service>();
        }

        var serviceUsersModule = serviceUsersModuleOption.Match(m => m, () => null!);
        var qsu13Field = serviceUsersModule.Fields.FirstOrDefault(f => f.FieldKey == "QSU13");

        if (qsu13Field == null)
        {
            return Array.Empty<Domain.Services.Service>();
        }

        var serviceSubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            serviceUsersModule.Id, organisationId, dataCollectionId, "Service", cancellationToken);

        var eligibleIds = serviceSubmissions
            .Where(s => s.EntityId.HasValue && s.FieldValues.Any(fv =>
                fv.FormFieldId == qsu13Field.Id &&
                string.Equals(fv.Value, "yes", StringComparison.OrdinalIgnoreCase)))
            .Select(s => s.EntityId!.Value)
            .ToHashSet();

        return allServices.Where(s => eligibleIds.Contains(s.Id.Value)).ToList();
    }

    private static List<BulkUploadFieldMetadataDto> BuildFieldMetadata(
        List<FormField> fields,
        IReadOnlyList<Domain.Services.Service> services,
        string moduleCode)
    {
        var metadata = new List<BulkUploadFieldMetadataDto>();

        // For service-users, prepend a synthetic ServiceName select so the frontend renders a
        // dropdown of valid services instead of a free-text input.
        if (moduleCode == DataCollectionFormModuleCodes.ServiceUsers)
        {
            var serviceOptions = services
                .OrderBy(s => s.Name)
                .Select(s => new BulkUploadFieldOptionDto(s.Name, s.Name))
                .ToList();

            metadata.Add(new BulkUploadFieldMetadataDto(
                "ServiceName",
                "Select",
                true,
                serviceOptions,
                null,
                null));
        }

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
        string moduleCode,
        Dictionary<string, string>? serviceAnswers)
    {
        var errors = new List<BulkUploadFieldErrorDto>();

        // For service-users, validate the ServiceName column.
        // Outcome scores uses PPS01 as the service field, validated as a regular field with dynamic options.
        if (moduleCode == DataCollectionFormModuleCodes.ServiceUsers)
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
            if (!IsFieldVisible(field, rowData, fields, serviceAnswers))
            {
                continue;
            }

            var value = rowData.GetValueOrDefault(field.FieldKey);
            var fieldErrors = BulkUploadFieldValueValidator.ValidateFieldValue(value, field, serviceNames);

            foreach (var error in fieldErrors)
            {
                errors.Add(new BulkUploadFieldErrorDto(field.FieldKey, field.Label, error));
            }
        }

        return errors;
    }

    private static bool IsFieldVisible(
        FormField field,
        Dictionary<string, string> rowData,
        List<FormField> allFields,
        Dictionary<string, string>? serviceAnswers)
        => ConditionalRuleEvaluator.IsFieldVisible(field, rowData, allFields, serviceAnswers);
}