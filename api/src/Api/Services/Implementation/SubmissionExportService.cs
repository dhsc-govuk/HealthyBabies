using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Submissions.Helpers;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.Organisations;
using LanguageExt;

namespace Api.Services.Implementation;

public class SubmissionExportService(
    IDataCollectionQueries dataCollectionQueries,
    IDataCollectionFormModuleQueries formModuleQueries,
    IFormSubmissionRepository formSubmissionRepository,
    IServiceQueries serviceQueries,
    IOrganisationQueries organisationQueries,
    IDataCollectionSubmissionQueries submissionQueries)
    : ISubmissionExportService
{
    public async Task<Either<string, byte[]>> ExportSubmissionAsCsvZipAsync(
        Guid dataCollectionId,
        Guid localAuthorityId,
        CancellationToken cancellationToken = default)
    {
        var dcId = new DataCollectionId(dataCollectionId);
        var orgId = new OrganisationId(localAuthorityId);

        var dataCollectionOption = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Either<string, byte[]>.Left("Data collection not found");
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        var organisationOption = await organisationQueries.GetOrganisationById(orgId, cancellationToken);
        if (organisationOption.IsNone)
        {
            return Either<string, byte[]>.Left("Local authority not found");
        }

        var organisation = organisationOption.Match(o => o, () => null!);
        var context = new CsvExportContext(organisation.Name, organisation.ONSCode, dataCollection.Name);

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var assignment in dataCollection.FormModuleAssignments.OrderBy(a => a.FormModule?.SectionNumber))
            {
                if (assignment.FormModule == null || !assignment.FormModule.IsActive)
                {
                    continue;
                }

                var formModuleWithFields = await formModuleQueries.GetByIdWithFieldsAsync(
                    assignment.FormModuleId, cancellationToken);

                if (formModuleWithFields.IsNone)
                {
                    continue;
                }

                var formModule = formModuleWithFields.Match(fm => fm, () => null!);
                var csvContent = await GenerateCsvForModuleAsync(
                    formModule, orgId, dcId, dataCollection.StartDate, context, cancellationToken);

                var fileName = SanitizeFileName($"{formModule.SectionNumber}_{formModule.Code}.csv");
                var entry = archive.CreateEntry(fileName);

                using var entryStream = entry.Open();
                var bytes = Encoding.UTF8.GetBytes(csvContent);
                await entryStream.WriteAsync(bytes, cancellationToken);
            }
        }

        memoryStream.Position = 0;
        return Either<string, byte[]>.Right(memoryStream.ToArray());
    }

    public async Task<Either<string, byte[]>> ExportSubmissionAsJsonAsync(
        Guid dataCollectionId,
        Guid localAuthorityId,
        CancellationToken cancellationToken = default)
    {
        var dcId = new DataCollectionId(dataCollectionId);
        var orgId = new OrganisationId(localAuthorityId);

        var dataCollectionOption = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Either<string, byte[]>.Left("Data collection not found");
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        var organisationOption = await organisationQueries.GetOrganisationById(orgId, cancellationToken);
        if (organisationOption.IsNone)
        {
            return Either<string, byte[]>.Left("Local authority not found");
        }

        var organisation = organisationOption.Match(o => o, () => null!);

        var exportData = new Dictionary<string, object>
        {
            ["dataCollectionId"] = dataCollectionId,
            ["dataCollectionName"] = dataCollection.Name,
            ["localAuthorityId"] = localAuthorityId,
            ["localAuthorityName"] = organisation.Name,
            ["exportedAt"] = DateTime.UtcNow,
            ["sections"] = new List<object>()
        };

        var sections = (List<object>)exportData["sections"];

        foreach (var assignment in dataCollection.FormModuleAssignments.OrderBy(a => a.FormModule?.SectionNumber))
        {
            if (assignment.FormModule == null || !assignment.FormModule.IsActive)
            {
                continue;
            }

            var formModuleWithFields = await formModuleQueries.GetByIdWithFieldsAsync(
                assignment.FormModuleId, cancellationToken);

            if (formModuleWithFields.IsNone)
            {
                continue;
            }

            var formModule = formModuleWithFields.Match(fm => fm, () => null!);
            var sectionData = await GenerateJsonForModuleAsync(
                formModule, orgId, dcId, dataCollection.StartDate, cancellationToken);

            sections.Add(sectionData);
        }

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return Either<string, byte[]>.Right(jsonBytes);
    }

    public async Task<Either<string, byte[]>> ExportSubmissionAsZipAsync(
        Guid dataCollectionId,
        Guid localAuthorityId,
        CancellationToken cancellationToken = default)
    {
        var dcId = new DataCollectionId(dataCollectionId);
        var orgId = new OrganisationId(localAuthorityId);

        var dataCollectionOption = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Either<string, byte[]>.Left("Data collection not found");
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        var organisationOption = await organisationQueries.GetOrganisationById(orgId, cancellationToken);
        if (organisationOption.IsNone)
        {
            return Either<string, byte[]>.Left("Local authority not found");
        }

        var organisation = organisationOption.Match(o => o, () => null!);
        var context = new CsvExportContext(organisation.Name, organisation.ONSCode, dataCollection.Name);

        var exportData = new Dictionary<string, object>
        {
            ["dataCollectionId"] = dataCollectionId,
            ["dataCollectionName"] = dataCollection.Name,
            ["localAuthorityId"] = localAuthorityId,
            ["localAuthorityName"] = organisation.Name,
            ["exportedAt"] = DateTime.UtcNow,
            ["sections"] = new List<object>()
        };
        var sections = (List<object>)exportData["sections"];

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var assignment in dataCollection.FormModuleAssignments.OrderBy(a => a.FormModule?.SectionNumber))
            {
                if (assignment.FormModule == null || !assignment.FormModule.IsActive)
                {
                    continue;
                }

                var formModuleWithFields = await formModuleQueries.GetByIdWithFieldsAsync(
                    assignment.FormModuleId, cancellationToken);

                if (formModuleWithFields.IsNone)
                {
                    continue;
                }

                var formModule = formModuleWithFields.Match(fm => fm, () => null!);

                var csvContent = await GenerateCsvForModuleAsync(
                    formModule, orgId, dcId, dataCollection.StartDate, context, cancellationToken);
                var csvFileName = SanitizeFileName($"{formModule.SectionNumber}_{formModule.Code}.csv");
                var csvEntry = archive.CreateEntry(csvFileName);
                using (var csvEntryStream = csvEntry.Open())
                {
                    var csvBytes = Encoding.UTF8.GetBytes(csvContent);
                    await csvEntryStream.WriteAsync(csvBytes, cancellationToken);
                }

                var sectionData = await GenerateJsonForModuleAsync(
                    formModule, orgId, dcId, dataCollection.StartDate, cancellationToken);
                sections.Add(sectionData);
            }

            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(exportData, new JsonSerializerOptions { WriteIndented = true });
            var jsonEntry = archive.CreateEntry("submission.json");
            using (var jsonEntryStream = jsonEntry.Open())
            {
                await jsonEntryStream.WriteAsync(jsonBytes, cancellationToken);
            }
        }

        memoryStream.Position = 0;
        return Either<string, byte[]>.Right(memoryStream.ToArray());
    }

    // BLUM-7361: "Download copy in CSV" on a module's record list (Outcome Scores, Service Users)
    // was wired to the bulk-upload *template* generator, which only pre-fills rows for services
    // still missing data - once every record for a service is approved/submitted there's nothing
    // "incomplete" left, so the download comes back as headers only. This exports the module's
    // actual records instead, reusing the same per-module CSV generator the full-submission export
    // already uses.
    public async Task<Either<string, byte[]>> ExportModuleAsCsvAsync(
        Guid dataCollectionId,
        Guid localAuthorityId,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        var dcId = new DataCollectionId(dataCollectionId);
        var orgId = new OrganisationId(localAuthorityId);
        var modId = new DataCollectionFormModuleId(moduleId);

        var dataCollectionOption = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Either<string, byte[]>.Left("Data collection not found");
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        var organisationOption = await organisationQueries.GetOrganisationById(orgId, cancellationToken);
        if (organisationOption.IsNone)
        {
            return Either<string, byte[]>.Left("Local authority not found");
        }

        var organisation = organisationOption.Match(o => o, () => null!);

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return Either<string, byte[]>.Left("Form module not found");
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);
        var context = new CsvExportContext(organisation.Name, organisation.ONSCode, dataCollection.Name);

        var csvContent = await GenerateCsvForModuleAsync(
            formModule, orgId, dcId, dataCollection.StartDate, context, cancellationToken);

        return Either<string, byte[]>.Right(Encoding.UTF8.GetBytes(csvContent));
    }

    public async Task<Either<string, byte[]>> ExportConsolidatedCsvZipAsync(
        Guid dataCollectionId,
        CancellationToken cancellationToken = default)
    {
        var dcId = new DataCollectionId(dataCollectionId);

        var dataCollectionOption = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Either<string, byte[]>.Left("Data collection not found");
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        var activeAssignments = dataCollection.FormModuleAssignments
            .Where(a => a.FormModule != null && a.FormModule.IsActive)
            .OrderBy(a => a.FormModule?.SectionNumber)
            .ToList();
        var formModulesById = await GetFormModulesByIdAsync(activeAssignments, cancellationToken);

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var exportOrgs = await GetExportOrganisationsAsync(dataCollection, dcId, cancellationToken);

            foreach (var org in exportOrgs)
            {
                var orgId = org.OrgId;
                var sanitizedOrgName = SanitizeFileName(org.Name);
                var context = new CsvExportContext(org.Name, org.ONSCode, dataCollection.Name);

                foreach (var assignment in activeAssignments)
                {
                    if (!formModulesById.TryGetValue(assignment.FormModuleId, out var formModule))
                    {
                        continue;
                    }

                    var csvContent = await GenerateCsvForModuleAsync(
                        formModule, orgId, dcId, dataCollection.StartDate, context, cancellationToken);

                    var fileName = $"{sanitizedOrgName}/{formModule.SectionNumber}_{formModule.Code}.csv";
                    var entry = archive.CreateEntry(fileName);

                    using var entryStream = entry.Open();
                    var bytes = Encoding.UTF8.GetBytes(csvContent);
                    await entryStream.WriteAsync(bytes, cancellationToken);
                }
            }
        }

        memoryStream.Position = 0;
        return Either<string, byte[]>.Right(memoryStream.ToArray());
    }

    public async Task<Either<string, byte[]>> ExportConsolidatedJsonAsync(
        Guid dataCollectionId,
        CancellationToken cancellationToken = default)
    {
        var dcId = new DataCollectionId(dataCollectionId);

        var dataCollectionOption = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(dcId, cancellationToken);
        if (dataCollectionOption.IsNone)
        {
            return Either<string, byte[]>.Left("Data collection not found");
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);

        var exportData = new Dictionary<string, object>
        {
            ["dataCollectionId"] = dataCollectionId,
            ["dataCollectionName"] = dataCollection.Name,
            ["exportedAt"] = DateTime.UtcNow,
            ["localAuthorities"] = new List<object>()
        };

        var localAuthorities = (List<object>)exportData["localAuthorities"];

        var activeAssignments = dataCollection.FormModuleAssignments
            .Where(a => a.FormModule != null && a.FormModule.IsActive)
            .OrderBy(a => a.FormModule?.SectionNumber)
            .ToList();
        var formModulesById = await GetFormModulesByIdAsync(activeAssignments, cancellationToken);

        var exportOrgs = await GetExportOrganisationsAsync(dataCollection, dcId, cancellationToken);

        foreach (var org in exportOrgs)
        {
            var orgId = org.OrgId;

            var laData = new Dictionary<string, object>
            {
                ["localAuthorityId"] = orgId.Value,
                ["localAuthorityName"] = org.Name,
                ["sections"] = new List<object>()
            };

            var sections = (List<object>)laData["sections"];

            foreach (var assignment in activeAssignments)
            {
                if (!formModulesById.TryGetValue(assignment.FormModuleId, out var formModule))
                {
                    continue;
                }

                var sectionData = await GenerateJsonForModuleAsync(
                    formModule, orgId, dcId, dataCollection.StartDate, cancellationToken);

                sections.Add(sectionData);
            }

            localAuthorities.Add(laData);
        }

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return Either<string, byte[]>.Right(jsonBytes);
    }

    private async Task<string> GenerateCsvForModuleAsync(
        DataCollectionFormModule formModule,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        DateTime collectionStartDate,
        CsvExportContext context,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        var fields = formModule.Fields
            .Where(f => f.IsActive)
            .OrderBy(f => formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber ?? 0)
            .ThenBy(f => f.DisplayOrder)
            .ToList();

        var moduleCode = formModule.Code;

        if (moduleCode == DataCollectionFormModuleCodes.ServiceUsers)
        {
            return await GenerateServiceUsersCsvAsync(formModule, fields, organisationId, dataCollectionId, collectionStartDate, context, cancellationToken);
        }

        if (moduleCode == DataCollectionFormModuleCodes.WiderServiceUsers)
        {
            return await GenerateWiderServiceUsersCsvAsync(formModule, fields, organisationId, dataCollectionId, context, cancellationToken);
        }

        if (moduleCode == DataCollectionFormModuleCodes.OutcomeScores)
        {
            return await GenerateOutcomeScoresCsvAsync(formModule, fields, organisationId, dataCollectionId, context, cancellationToken);
        }

        if (formModule.AllowsMultipleActivities)
        {
            return await GenerateActivitiesCsvAsync(formModule, fields, organisationId, dataCollectionId, context, cancellationToken);
        }

        return await GenerateStandardFormCsvAsync(formModule, fields, organisationId, dataCollectionId, context, cancellationToken);
    }

    // See GenerateActivitiesJsonAsync — same reasoning: a module with AllowsMultipleActivities can
    // have more than one submission per (module, organisation, data collection), so every activity
    // gets its own row block instead of only exporting the single most-recently-created one.
    private async Task<string> GenerateActivitiesCsvAsync(
        DataCollectionFormModule formModule,
        List<FormField> fields,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CsvExportContext context,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        var headers = new List<string> { "LAName", "ONSCode", "CollectionPeriod", "ActivityId", "ActivityName", "Status", "QuestionCode", "Question", "Answer" };
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

        var activitySubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            formModule.Id, organisationId, dataCollectionId, "Activity", cancellationToken);

        foreach (var submission in activitySubmissions)
        {
            var fieldValues = submission.FieldValues.ToDictionary(fv => fv.FormFieldId);

            foreach (var field in fields)
            {
                var fieldValue = fieldValues.TryGetValue(field.Id, out var fv) ? fv : null;
                var answer = AnswerSnapshotResolver.ResolveDisplayValue(fieldValue, field) ?? string.Empty;
                var row = new List<string>
                {
                    context.LaName,
                    context.OnsCode,
                    context.CollectionPeriod,
                    submission.Id.Value.ToString(),
                    submission.Name ?? string.Empty,
                    submission.Status.Value,
                    field.FieldKey,
                    AnswerSnapshotResolver.ResolveQuestionLabel(fieldValue) ?? field.Label,
                    answer
                };
                sb.AppendLine(string.Join(",", row.Select(EscapeCsvField)));
            }
        }

        return sb.ToString();
    }

    private async Task<string> GenerateStandardFormCsvAsync(
        DataCollectionFormModule formModule,
        List<FormField> fields,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CsvExportContext context,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        var headers = new List<string> { "LAName", "ONSCode", "CollectionPeriod", "QuestionCode", "Question", "Answer" };
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

        var submissions = await formSubmissionRepository.GetByFormModuleOrganisationAndDataCollectionAsync(
            formModule.Id, organisationId, dataCollectionId, cancellationToken);

        var submission = submissions.FirstOrDefault();
        var fieldValues = submission?.FieldValues.ToDictionary(fv => fv.FormFieldId)
            ?? new Dictionary<FormFieldId, FormFieldValue>();

        foreach (var field in fields)
        {
            var fieldValue = fieldValues.TryGetValue(field.Id, out var fv) ? fv : null;
            var answer = AnswerSnapshotResolver.ResolveDisplayValue(fieldValue, field) ?? string.Empty;
            var row = new List<string>
            {
                context.LaName,
                context.OnsCode,
                context.CollectionPeriod,
                field.FieldKey,
                AnswerSnapshotResolver.ResolveQuestionLabel(fieldValue) ?? field.Label,
                answer
            };
            sb.AppendLine(string.Join(",", row.Select(EscapeCsvField)));
        }

        return sb.ToString();
    }

    private async Task<string> GenerateServiceUsersCsvAsync(
        DataCollectionFormModule formModule,
        List<FormField> fields,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        DateTime collectionStartDate,
        CsvExportContext context,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        var headers = new List<string> { "LAName", "ONSCode", "CollectionPeriod", "ServiceId", "ServiceName" };
        headers.AddRange(fields.Select(f => f.FieldKey));
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

        var labelRow = new List<string> { "LA Name", "ONS Code", "Collection Period", "Service ID", "Service Name" };
        labelRow.AddRange(fields.Select(f => f.Label));
        sb.AppendLine(string.Join(",", labelRow.Select(EscapeCsvField)));

        var allServices = await serviceQueries.GetByOrganisationIdAsync(organisationId, cancellationToken);
        var services = allServices
            .Where(s => ServiceQuarterlyInclusion.IsIncludedInQuarterlyServiceUsers(s, collectionStartDate))
            .ToList();
        var serviceSubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            formModule.Id, organisationId, dataCollectionId, "Service", cancellationToken);

        foreach (var service in services)
        {
            var submission = serviceSubmissions.FirstOrDefault(s => s.EntityId == service.Id.Value);
            var fieldValues = submission?.FieldValues.ToDictionary(fv => fv.FormFieldId)
                ?? new Dictionary<FormFieldId, FormFieldValue>();

            var row = new List<string> { context.LaName, context.OnsCode, context.CollectionPeriod, service.Id.Value.ToString(), service.Name ?? string.Empty };
            foreach (var field in fields)
            {
                var answer = fieldValues.TryGetValue(field.Id, out var fv)
                    ? AnswerSnapshotResolver.ResolveDisplayValue(fv, field) ?? string.Empty
                    : string.Empty;
                row.Add(answer);
            }

            sb.AppendLine(string.Join(",", row.Select(EscapeCsvField)));
        }

        return sb.ToString();
    }

    private async Task<string> GenerateWiderServiceUsersCsvAsync(
        DataCollectionFormModule formModule,
        List<FormField> fields,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CsvExportContext context,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        var headers = new List<string> { "LAName", "ONSCode", "CollectionPeriod", "CategoryName" };
        headers.AddRange(fields.Select(f => f.FieldKey));
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

        var labelRow = new List<string> { "LA Name", "ONS Code", "Collection Period", "Category Name" };
        labelRow.AddRange(fields.Select(f => f.Label));
        sb.AppendLine(string.Join(",", labelRow.Select(EscapeCsvField)));

        var serviceCategories = await serviceQueries.GetServiceCategoriesByOrganisationIdAsync(organisationId, cancellationToken);

        var filteredCategories = serviceCategories
            .Where(c =>
            {
                var wsdm01Answer = c.Answers.FirstOrDefault(a => a.QuestionCode == "WSDM01");
                return wsdm01Answer == null || wsdm01Answer.Value != "no";
            })
            .ToList();

        var categorySubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            formModule.Id, organisationId, dataCollectionId, "WiderServiceCategory", cancellationToken);

        foreach (var category in filteredCategories)
        {
            var submission = categorySubmissions.FirstOrDefault(s => s.EntityId == category.Id.Value);
            var fieldValues = submission?.FieldValues.ToDictionary(fv => fv.FormFieldId)
                ?? new Dictionary<FormFieldId, FormFieldValue>();

            var row = new List<string> { context.LaName, context.OnsCode, context.CollectionPeriod, category.CategoryName ?? string.Empty };
            foreach (var field in fields)
            {
                var answer = fieldValues.TryGetValue(field.Id, out var fv)
                    ? AnswerSnapshotResolver.ResolveDisplayValue(fv, field) ?? string.Empty
                    : string.Empty;
                row.Add(answer);
            }

            sb.AppendLine(string.Join(",", row.Select(EscapeCsvField)));
        }

        return sb.ToString();
    }

    private async Task<string> GenerateOutcomeScoresCsvAsync(
        DataCollectionFormModule formModule,
        List<FormField> fields,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CsvExportContext context,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        var headers = new List<string> { "LAName", "ONSCode", "CollectionPeriod", "ServiceId", "RecordId" };
        headers.AddRange(fields.Select(f => f.FieldKey));
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

        var labelRow = new List<string> { "LA Name", "ONS Code", "Collection Period", "Service ID", "Record ID" };
        labelRow.AddRange(fields.Select(f => f.Label));
        sb.AppendLine(string.Join(",", labelRow.Select(EscapeCsvField)));

        var outcomeSubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            formModule.Id, organisationId, dataCollectionId, "OutcomeScore", cancellationToken);

        var services = await serviceQueries.GetByOrganisationIdAsync(organisationId, cancellationToken);
        var serviceNameLookup = services.ToDictionary(s => s.Id.Value.ToString(), s => s.Name ?? string.Empty);

        foreach (var submission in outcomeSubmissions)
        {
            var fieldValues = submission.FieldValues.ToDictionary(fv => fv.FormFieldId);

            var row = new List<string> { context.LaName, context.OnsCode, context.CollectionPeriod, submission.EntityId?.ToString() ?? string.Empty, submission.Id.Value.ToString() };
            foreach (var field in fields)
            {
                var answer = fieldValues.TryGetValue(field.Id, out var fv)
                    ? AnswerSnapshotResolver.ResolveDisplayValue(fv, field) ?? string.Empty
                    : string.Empty;

                // Any service-backed field, single or multi-value — not just Outcome Scores' PPS01.
                if (DynamicServiceOptions.IsServiceBacked(field) && !string.IsNullOrEmpty(answer))
                {
                    answer = DynamicServiceOptions.ResolveDisplayValue(answer, serviceNameLookup);
                }

                row.Add(answer);
            }

            sb.AppendLine(string.Join(",", row.Select(EscapeCsvField)));
        }

        return sb.ToString();
    }

    private async Task<object> GenerateJsonForModuleAsync(
        DataCollectionFormModule formModule,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        DateTime collectionStartDate,
        CancellationToken cancellationToken)
    {
        var fields = formModule.Fields
            .Where(f => f.IsActive)
            .OrderBy(f => formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber ?? 0)
            .ThenBy(f => f.DisplayOrder)
            .ToList();
        var moduleCode = formModule.Code;

        var sectionData = new Dictionary<string, object>
        {
            ["moduleCode"] = moduleCode,
            ["moduleName"] = formModule.Name,
            ["sectionNumber"] = formModule.SectionNumber
        };

        if (moduleCode == DataCollectionFormModuleCodes.ServiceUsers)
        {
            sectionData["records"] = await GenerateServiceUsersJsonAsync(formModule, fields, organisationId, dataCollectionId, collectionStartDate, cancellationToken);
        }
        else if (moduleCode == DataCollectionFormModuleCodes.WiderServiceUsers)
        {
            sectionData["records"] = await GenerateWiderServiceUsersJsonAsync(formModule, fields, organisationId, dataCollectionId, cancellationToken);
        }
        else if (moduleCode == DataCollectionFormModuleCodes.OutcomeScores)
        {
            sectionData["records"] = await GenerateOutcomeScoresJsonAsync(formModule, fields, organisationId, dataCollectionId, cancellationToken);
        }
        else if (formModule.AllowsMultipleActivities)
        {
            sectionData["activities"] = await GenerateActivitiesJsonAsync(formModule, fields, organisationId, dataCollectionId, cancellationToken);
        }
        else
        {
            sectionData["answers"] = await GenerateStandardFormJsonAsync(formModule, fields, organisationId, dataCollectionId, cancellationToken);
        }

        return sectionData;
    }

    // Activity modules (Delivery Plan strands with AllowsMultipleActivities) can have more than one
    // submission per (module, organisation, data collection) — unlike GenerateStandardFormJsonAsync,
    // which only ever exports a single instance, this exports every activity as its own entry so none
    // are silently dropped.
    private async Task<List<Dictionary<string, object>>> GenerateActivitiesJsonAsync(
        DataCollectionFormModule formModule,
        List<FormField> fields,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken)
    {
        var activitySubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            formModule.Id, organisationId, dataCollectionId, "Activity", cancellationToken);

        var activities = new List<Dictionary<string, object>>();
        foreach (var submission in activitySubmissions)
        {
            var fieldValues = submission.FieldValues.ToDictionary(fv => fv.FormFieldId);

            activities.Add(new Dictionary<string, object>
            {
                ["activityId"] = submission.Id.Value,
                ["activityName"] = submission.Name ?? string.Empty,
                ["status"] = submission.Status.Value,
                ["answers"] = fields.Select(f =>
                {
                    var fieldValue = fieldValues.TryGetValue(f.Id, out var fv) ? fv : null;

                    return new Dictionary<string, object>
                    {
                        ["questionCode"] = f.FieldKey,
                        ["question"] = AnswerSnapshotResolver.ResolveQuestionLabel(fieldValue) ?? f.Label,
                        ["answer"] = AnswerSnapshotResolver.ResolveDisplayValue(fieldValue, f) ?? string.Empty,
                        // Raw stored value alongside the resolved display text, so a later consumer (the
                        // admin's previous-submissions review) can key a tag colour off the option value
                        // rather than matching on the English label.
                        ["value"] = fieldValue?.Value ?? string.Empty
                    };
                }).ToList()
            });
        }

        return activities;
    }

    private async Task<List<Dictionary<string, object>>> GenerateStandardFormJsonAsync(
        DataCollectionFormModule formModule,
        List<FormField> fields,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken)
    {
        var submissions = await formSubmissionRepository.GetByFormModuleOrganisationAndDataCollectionAsync(
            formModule.Id, organisationId, dataCollectionId, cancellationToken);

        var submission = submissions.FirstOrDefault();
        var fieldValues = submission?.FieldValues.ToDictionary(fv => fv.FormFieldId)
            ?? new Dictionary<FormFieldId, FormFieldValue>();

        var answers = new List<Dictionary<string, object>>();
        foreach (var field in fields)
        {
            var fieldValue = fieldValues.TryGetValue(field.Id, out var fv) ? fv : null;
            var answer = AnswerSnapshotResolver.ResolveDisplayValue(fieldValue, field) ?? string.Empty;
            answers.Add(new Dictionary<string, object>
            {
                ["questionCode"] = field.FieldKey,
                ["question"] = AnswerSnapshotResolver.ResolveQuestionLabel(fieldValue) ?? field.Label,
                ["answer"] = answer
            });
        }

        return answers;
    }

    private async Task<List<Dictionary<string, object>>> GenerateServiceUsersJsonAsync(
        DataCollectionFormModule formModule,
        List<FormField> fields,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        DateTime collectionStartDate,
        CancellationToken cancellationToken)
    {
        var allServices = await serviceQueries.GetByOrganisationIdAsync(organisationId, cancellationToken);
        var services = allServices
            .Where(s => ServiceQuarterlyInclusion.IsIncludedInQuarterlyServiceUsers(s, collectionStartDate))
            .ToList();
        var serviceSubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            formModule.Id, organisationId, dataCollectionId, "Service", cancellationToken);

        var records = new List<Dictionary<string, object>>();
        foreach (var service in services)
        {
            var submission = serviceSubmissions.FirstOrDefault(s => s.EntityId == service.Id.Value);
            var fieldValues = submission?.FieldValues.ToDictionary(fv => fv.FormFieldId)
                ?? new Dictionary<FormFieldId, FormFieldValue>();

            var record = new Dictionary<string, object>
            {
                ["serviceId"] = service.Id.Value,
                ["serviceName"] = service.Name ?? string.Empty,
                ["answers"] = fields.Select(f =>
                {
                    var fieldValue = fieldValues.TryGetValue(f.Id, out var fv) ? fv : null;

                    return new Dictionary<string, object>
                    {
                        ["questionCode"] = f.FieldKey,
                        ["question"] = AnswerSnapshotResolver.ResolveQuestionLabel(fieldValue) ?? f.Label,
                        ["answer"] = AnswerSnapshotResolver.ResolveDisplayValue(fieldValue, f) ?? string.Empty
                    };
                }).ToList()
            };

            records.Add(record);
        }

        return records;
    }

    private async Task<List<Dictionary<string, object>>> GenerateWiderServiceUsersJsonAsync(
        DataCollectionFormModule formModule,
        List<FormField> fields,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken)
    {
        var serviceCategories = await serviceQueries.GetServiceCategoriesByOrganisationIdAsync(organisationId, cancellationToken);

        var filteredCategories = serviceCategories
            .Where(c =>
            {
                var wsdm01Answer = c.Answers.FirstOrDefault(a => a.QuestionCode == "WSDM01");
                return wsdm01Answer == null || wsdm01Answer.Value != "no";
            })
            .ToList();

        var categorySubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            formModule.Id, organisationId, dataCollectionId, "WiderServiceCategory", cancellationToken);

        var records = new List<Dictionary<string, object>>();
        foreach (var category in filteredCategories)
        {
            var submission = categorySubmissions.FirstOrDefault(s => s.EntityId == category.Id.Value);
            var fieldValues = submission?.FieldValues.ToDictionary(fv => fv.FormFieldId)
                ?? new Dictionary<FormFieldId, FormFieldValue>();

            var record = new Dictionary<string, object>
            {
                ["categoryId"] = category.Id.Value,
                ["categoryName"] = category.CategoryName ?? string.Empty,
                ["answers"] = fields.Select(f =>
                {
                    var fieldValue = fieldValues.TryGetValue(f.Id, out var fv) ? fv : null;

                    return new Dictionary<string, object>
                    {
                        ["questionCode"] = f.FieldKey,
                        ["question"] = AnswerSnapshotResolver.ResolveQuestionLabel(fieldValue) ?? f.Label,
                        ["answer"] = AnswerSnapshotResolver.ResolveDisplayValue(fieldValue, f) ?? string.Empty
                    };
                }).ToList()
            };

            records.Add(record);
        }

        return records;
    }

    private async Task<List<Dictionary<string, object>>> GenerateOutcomeScoresJsonAsync(
        DataCollectionFormModule formModule,
        List<FormField> fields,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken)
    {
        var outcomeSubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            formModule.Id, organisationId, dataCollectionId, "OutcomeScore", cancellationToken);

        var services = await serviceQueries.GetByOrganisationIdAsync(organisationId, cancellationToken);
        var serviceNameLookup = services.ToDictionary(s => s.Id.Value.ToString(), s => s.Name ?? string.Empty);

        var records = new List<Dictionary<string, object>>();
        foreach (var submission in outcomeSubmissions)
        {
            var fieldValues = submission.FieldValues.ToDictionary(fv => fv.FormFieldId);

            var record = new Dictionary<string, object>
            {
                ["recordId"] = submission.Id.Value,
                ["answers"] = fields.Select(f =>
                {
                    var fieldValue = fieldValues.TryGetValue(f.Id, out var fv) ? fv : null;
                    var answer = AnswerSnapshotResolver.ResolveDisplayValue(fieldValue, f) ?? string.Empty;

                    if (DynamicServiceOptions.IsServiceBacked(f) && !string.IsNullOrEmpty(answer))
                    {
                        answer = DynamicServiceOptions.ResolveDisplayValue(answer, serviceNameLookup);
                    }

                    return new Dictionary<string, object>
                    {
                        ["questionCode"] = f.FieldKey,
                        ["question"] = AnswerSnapshotResolver.ResolveQuestionLabel(fieldValue) ?? f.Label,
                        ["answer"] = answer
                    };
                }).ToList()
            };

            records.Add(record);
        }

        return records;
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

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }

    private async Task<List<ExportOrganisation>> GetExportOrganisationsAsync(
        DataCollection dataCollection,
        DataCollectionId dcId,
        CancellationToken cancellationToken)
    {
        var orgMap = new Dictionary<Guid, ExportOrganisation>();

        // Add explicitly assigned LAs
        foreach (var la in dataCollection.LocalAuthorities)
        {
            orgMap[la.LocalAuthorityId.Value] = new ExportOrganisation(
                la.LocalAuthorityId,
                la.LocalAuthority?.Name ?? string.Empty,
                la.LocalAuthority?.ONSCode ?? string.Empty);
        }

        // Add orgs from submissions (catches non-assigned LAs)
        var allSubmissions = await submissionQueries.GetByDataCollectionIdAsync(dcId, cancellationToken);
        foreach (var sub in allSubmissions)
        {
            if (!orgMap.ContainsKey(sub.OrganisationId.Value))
            {
                orgMap[sub.OrganisationId.Value] = new ExportOrganisation(
                    sub.OrganisationId,
                    sub.Organisation?.Name ?? string.Empty,
                    sub.Organisation?.ONSCode ?? string.Empty);
            }
        }

        return orgMap.Values.OrderBy(o => o.Name).ToList();
    }

    private record ExportOrganisation(OrganisationId OrgId, string Name, string ONSCode);

    // Modules/fields are identical across every organisation in the export, so fetch each assigned module
    // once here rather than once per (organisation, module) pair inside the export loop.
    private async Task<Dictionary<DataCollectionFormModuleId, DataCollectionFormModule>> GetFormModulesByIdAsync(
        IReadOnlyCollection<DataCollectionFormModuleAssignment> assignments,
        CancellationToken cancellationToken)
    {
        var formModulesById = new Dictionary<DataCollectionFormModuleId, DataCollectionFormModule>();

        foreach (var moduleId in assignments.Select(a => a.FormModuleId).Distinct())
        {
            var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(moduleId, cancellationToken);
            // MatchUnsafe, not Match: the null below is the intended "module could not be loaded" result,
            // and Match raises ResultIsNullException on a null result rather than returning it (BLUM-6773).
            var formModule = formModuleOption.MatchUnsafe(fm => fm, () => (DataCollectionFormModule?)null);
            if (formModule != null)
            {
                formModulesById[moduleId] = formModule;
            }
        }

        return formModulesById;
    }

    private record CsvExportContext(string LaName, string OnsCode, string CollectionPeriod);
}