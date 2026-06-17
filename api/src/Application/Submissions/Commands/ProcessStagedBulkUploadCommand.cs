using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using LanguageExt;
using MediatR;

namespace Application.Submissions.Commands;

public record BulkUploadCellEdit(int RowIndex, int ColumnIndex, string Value);

public record ProcessStagedBulkUploadCommand : IRequest<Either<string, BulkUploadResultDto>>
{
    public Guid SubmissionId { get; init; }
    public Guid ModuleId { get; init; }
    public Guid StagingId { get; init; }
    public IReadOnlyList<string> SelectedServiceNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<BulkUploadCellEdit> CellEdits { get; init; } = Array.Empty<BulkUploadCellEdit>();
}

public class ProcessStagedBulkUploadCommandHandler(
    PermissionsService permissionsService,
    IDataCollectionFormModuleQueries formModuleQueries,
    IServiceQueries serviceQueries,
    IFormSubmissionRepository formSubmissionRepository,
    IBlobService blobService)
    : IRequestHandler<ProcessStagedBulkUploadCommand, Either<string, BulkUploadResultDto>>
{
    public async Task<Either<string, BulkUploadResultDto>> Handle(
        ProcessStagedBulkUploadCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();

        return await permissions.MatchAsync(
            async p => await ProcessUpload(p, request, cancellationToken),
            e => (Either<string, BulkUploadResultDto>)e.Message);
    }

    private async Task<Either<string, BulkUploadResultDto>> ProcessUpload(
        Permission permission,
        ProcessStagedBulkUploadCommand request,
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

        var streamResult = await blobService.GetReadStream(
            BulkUploadCsvHelper.BulkUploadStagingContainer,
            $"{request.StagingId}.csv",
            cancellationToken);

        return await streamResult.MatchAsync<Either<string, BulkUploadResultDto>>(
            async stream =>
            {
                List<List<string>> rows;
                await using (stream)
                {
                    rows = await BulkUploadCsvHelper.ParseCsvAsync(stream, cancellationToken);
                }

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

                ApplyCellEdits(dataRows, request.CellEdits);

                var filtered = FilterBySelectedServices(headers, dataRows, request.SelectedServiceNames);

                var processor = new BulkUploadRowProcessor(formSubmissionRepository);
                var result = await processor.ProcessAsync(
                    formModule,
                    fields,
                    serviceMap,
                    modId,
                    orgId,
                    dcId,
                    headers,
                    filtered,
                    dataStartRowNumber: 2,
                    cancellationToken);

                await blobService.RemoveFile(
                    BulkUploadCsvHelper.BulkUploadStagingContainer,
                    $"{request.StagingId}.csv",
                    cancellationToken);

                return result;
            },
            _ => Task.FromResult<Either<string, BulkUploadResultDto>>(
                "Upload session expired or not found, please re-upload"));
    }

    private static void ApplyCellEdits(List<List<string>> dataRows, IReadOnlyList<BulkUploadCellEdit> edits)
    {
        foreach (var edit in edits)
        {
            if (edit.RowIndex < 0 || edit.RowIndex >= dataRows.Count)
            {
                continue;
            }

            var row = dataRows[edit.RowIndex];

            while (row.Count <= edit.ColumnIndex)
            {
                row.Add(string.Empty);
            }

            if (edit.ColumnIndex >= 0)
            {
                row[edit.ColumnIndex] = edit.Value;
            }
        }
    }

    private static List<List<string>> FilterBySelectedServices(
        List<string> headers,
        List<List<string>> dataRows,
        IReadOnlyList<string> selectedServiceNames)
    {
        if (selectedServiceNames.Count == 0)
        {
            return dataRows;
        }

        var serviceNameIndex = headers.FindIndex(h => h.Equals("ServiceName", StringComparison.OrdinalIgnoreCase));
        if (serviceNameIndex < 0)
        {
            serviceNameIndex = headers.FindIndex(h => h.Equals("PPS01", StringComparison.OrdinalIgnoreCase));
        }

        if (serviceNameIndex < 0)
        {
            return dataRows;
        }

        var selected = new System.Collections.Generic.HashSet<string>(selectedServiceNames, StringComparer.OrdinalIgnoreCase);
        return dataRows
            .Where(row => serviceNameIndex < row.Count && selected.Contains(row[serviceNameIndex]))
            .ToList();
    }
}