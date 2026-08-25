using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.ValueObjects;
using LanguageExt;
using MediatR;

namespace Application.Submissions.Commands;

// One-off admin tool (BLUM-7454) for importing historical Delivery Plan activity data on behalf of
// a local authority, bypassing the normal own-org-only bulk-upload path. Deliberately a separate
// command from ProcessBulkUploadCommand rather than a modification to it: the org comes from the
// request instead of the caller's own permissions, the submission-window lock checks don't apply
// (the data being imported predates any window that could be open today), and every migrated row is
// stamped with the source's own effective date instead of "now".
public record ImportMigratedActivitiesCommand : IRequest<Either<string, MigrationImportResultDto>>
{
    public string OrganisationName { get; init; } = string.Empty;
    public Guid DataCollectionId { get; init; }
    public Guid ModuleId { get; init; }
    public DateTime SubmittedAtUtc { get; init; }
    public Stream FileStream { get; init; } = null!;
    public string FileName { get; init; } = string.Empty;
}

public class ImportMigratedActivitiesCommandHandler(
    PermissionsService permissionsService,
    IOrganisationRepository organisationRepository,
    IDataCollectionRepository dataCollectionRepository,
    IDataCollectionFormModuleQueries formModuleQueries,
    IFormSubmissionRepository formSubmissionRepository)
    : IRequestHandler<ImportMigratedActivitiesCommand, Either<string, MigrationImportResultDto>>
{
    public async Task<Either<string, MigrationImportResultDto>> Handle(
        ImportMigratedActivitiesCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();

        return await permissions.MatchAsync(
            async p => p.UserRole.Equals(UserRole.Admin)
                ? await ProcessImport(request, cancellationToken)
                : (Either<string, MigrationImportResultDto>)"Only Admin users can run migration imports",
            e => (Either<string, MigrationImportResultDto>)e.Message);
    }

    private async Task<Either<string, MigrationImportResultDto>> ProcessImport(
        ImportMigratedActivitiesCommand request,
        CancellationToken cancellationToken)
    {
        var orgOption = await organisationRepository.FindByNameAsync(request.OrganisationName, cancellationToken);
        if (orgOption.IsNone)
        {
            return $"Organisation '{request.OrganisationName}' not found";
        }

        var org = orgOption.Match(o => o, () => null!);
        var dcId = new DataCollectionId(request.DataCollectionId);

        var dcOption = await dataCollectionRepository.GetByIdWithLocalAuthoritiesAsync(dcId, cancellationToken);
        if (dcOption.IsNone)
        {
            return $"Data collection '{request.DataCollectionId}' not found";
        }

        var dataCollection = dcOption.Match(dc => dc, () => null!);
        if (!dataCollection.LocalAuthorities.Any(la => la.LocalAuthorityId == org.Id))
        {
            return $"'{request.OrganisationName}' is not assigned to this data collection - assign it first, then re-run the import";
        }

        var modId = new DataCollectionFormModuleId(request.ModuleId);
        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return $"Form module '{request.ModuleId}' not found";
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);
        var fields = formModule.Fields
            .Where(f => f.IsActive)
            .OrderBy(f => formModule.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber ?? 0)
            .ThenBy(f => f.DisplayOrder)
            .ToList();

        var rows = await BulkUploadCsvHelper.ParseCsvAsync(request.FileStream, cancellationToken);
        if (rows.Count < 2)
        {
            return new MigrationImportResultDto(
                org.Name,
                formModule.Code,
                request.SubmittedAtUtc,
                new BulkUploadResultDto(false, 0, 0, 0, new List<BulkUploadRowResultDto>()));
        }

        var headers = rows[0];
        var dataRows = rows.Skip(1).ToList();

        if (dataRows.Count > 0 && BulkUploadCsvHelper.IsLabelRow(dataRows[0], fields))
        {
            dataRows = dataRows.Skip(1).ToList();
        }

        if (dataRows.Count > 0 && BulkUploadCsvHelper.IsRequiredIndicatorsRow(dataRows[0]))
        {
            dataRows = dataRows.Skip(1).ToList();
        }

        // Activity modules (all Delivery Plan strand modules) never consult serviceMap - it's only
        // read by the non-activity branch of BulkUploadRowProcessor - so an empty map is safe here.
        var processor = new BulkUploadRowProcessor(formSubmissionRepository);
        var result = await processor.ProcessAsync(
            formModule,
            fields,
            new Dictionary<string, Domain.Services.Service>(),
            modId,
            org.Id,
            dcId,
            headers,
            dataRows,
            dataStartRowNumber: 2,
            cancellationToken,
            request.SubmittedAtUtc);

        return new MigrationImportResultDto(org.Name, formModule.Code, request.SubmittedAtUtc, result);
    }
}