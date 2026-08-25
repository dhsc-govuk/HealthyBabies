using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Submissions.Dtos;
using Application.Submissions.Helpers;
using Domain.DataCollections;
using LanguageExt;
using MediatR;

namespace Application.Submissions.Commands;

public record ProcessBulkUploadCommand : IRequest<Either<string, BulkUploadResultDto>>
{
    public Guid SubmissionId { get; init; }
    public Guid ModuleId { get; init; }
    public Stream FileStream { get; init; } = null!;
    public string FileName { get; init; } = string.Empty;
}

public class ProcessBulkUploadCommandHandler(
    PermissionsService permissionsService,
    IDataCollectionFormModuleQueries formModuleQueries,
    IServiceQueries serviceQueries,
    IFormSubmissionRepository formSubmissionRepository,
    IDataCollectionSubmissionQueries dataCollectionSubmissionQueries,
    IDataCollectionQueries dataCollectionQueries)
    : IRequestHandler<ProcessBulkUploadCommand, Either<string, BulkUploadResultDto>>
{
    private const string LockedMessage = "This quarter can no longer be edited - it has either already been submitted or its submission window has closed. Reopen it to make changes.";

    public async Task<Either<string, BulkUploadResultDto>> Handle(
        ProcessBulkUploadCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();

        return await permissions.MatchAsync(
            async p => await ProcessUpload(p, request, cancellationToken),
            e => (Either<string, BulkUploadResultDto>)e.Message);
    }

    private async Task<Either<string, BulkUploadResultDto>> ProcessUpload(
        Permission permission,
        ProcessBulkUploadCommand request,
        CancellationToken cancellationToken)
    {
        var modId = new DataCollectionFormModuleId(request.ModuleId);
        var orgId = permission.OrganisationId.Match(
            id => id,
            () => throw new InvalidOperationException("Organisation ID required"));
        var dcId = new DataCollectionId(request.SubmissionId);

        var dataCollectionOption = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(dcId, cancellationToken);
        var isClosed = dataCollectionOption.Match(dc => dc.IsClosedFor(orgId), () => false);

        var existingSubmission = await dataCollectionSubmissionQueries.GetByDataCollectionAndOrganisationAsync(
            dcId, orgId, cancellationToken);
        var isSubmitted = existingSubmission.Match(s => s.IsLockedForEditing(), () => false);

        if (isClosed || isSubmitted)
        {
            return LockedMessage;
        }

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return "Form module not found";
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);
        var fields = formModule.Fields
            .Where(f => f.IsActive)
            .OrderBy(f => formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber ?? 0)
            .ThenBy(f => f.DisplayOrder)
            .ToList();

        var services = await serviceQueries.GetByOrganisationIdAsync(orgId, cancellationToken);

        // Service names aren't unique in the DB, so group rather than ToDictionary (which throws on a
        // duplicate key) — when two services share a name, resolve the ambiguity deterministically.
        var serviceMap = services
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var rows = await BulkUploadCsvHelper.ParseCsvAsync(request.FileStream, cancellationToken);

        if (rows.Count < 2)
        {
            return new BulkUploadResultDto(false, 0, 0, 0, new List<BulkUploadRowResultDto>());
        }

        var headers = rows[0];
        var dataRows = rows.Skip(1).ToList();

        // Detect the preamble against the template's columns, not the full field list above:
        // validate split the file that way, and the client's row indices follow validate.
        var preambleRows = BulkUploadCsvHelper.CountPreambleRows(dataRows, BulkUploadTemplateColumns.ForModule(formModule));
        dataRows = dataRows.Skip(preambleRows).ToList();

        var processor = new BulkUploadRowProcessor(formSubmissionRepository);
        return await processor.ProcessAsync(
            formModule,
            fields,
            serviceMap,
            modId,
            orgId,
            dcId,
            headers,
            dataRows,
            dataStartRowNumber: preambleRows + 2,
            cancellationToken);
    }
}