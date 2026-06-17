using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Submissions.Dtos;
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
    IFormSubmissionRepository formSubmissionRepository)
    : IRequestHandler<ProcessBulkUploadCommand, Either<string, BulkUploadResultDto>>
{
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

        var formModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (formModuleOption.IsNone)
        {
            return "Form module not found";
        }

        var formModule = formModuleOption.Match(fm => fm, () => null!);
        var fields = formModule.Fields
            .Where(f => f.IsActive)
            .OrderBy(f => f.DisplayOrder)
            .ToList();

        var services = await serviceQueries.GetByOrganisationIdAsync(orgId, cancellationToken);
        var serviceMap = services.ToDictionary(s => s.Name.ToLowerInvariant(), s => s);

        var rows = await BulkUploadCsvHelper.ParseCsvAsync(request.FileStream, cancellationToken);

        if (rows.Count < 2)
        {
            return new BulkUploadResultDto(false, 0, 0, 0, new List<BulkUploadRowResultDto>());
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
            dataStartRowNumber: 2,
            cancellationToken);
    }
}