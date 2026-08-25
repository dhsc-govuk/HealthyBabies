using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Submissions.Dtos;
using Application.Submissions.Helpers;
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
    public IReadOnlyList<int> SelectedRowIndices { get; init; } = Array.Empty<int>();
    public IReadOnlyList<BulkUploadCellEdit> CellEdits { get; init; } = Array.Empty<BulkUploadCellEdit>();
}

public class ProcessStagedBulkUploadCommandHandler(
    PermissionsService permissionsService,
    IDataCollectionFormModuleQueries formModuleQueries,
    IServiceQueries serviceQueries,
    IFormSubmissionRepository formSubmissionRepository,
    IBlobService blobService,
    IDataCollectionSubmissionQueries dataCollectionSubmissionQueries,
    IDataCollectionQueries dataCollectionQueries)
    : IRequestHandler<ProcessStagedBulkUploadCommand, Either<string, BulkUploadResultDto>>
{
    private const string LockedMessage = "This quarter can no longer be edited - it has either already been submitted or its submission window has closed. Reopen it to make changes.";

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
                var skippedRows = 1;

                // Detect the preamble against the template's columns, not the full field list above:
                // validate split the file that way, and the client's row indices follow validate.
                var preambleRows = BulkUploadCsvHelper.CountPreambleRows(dataRows, BulkUploadTemplateColumns.ForModule(formModule));
                dataRows = dataRows.Skip(preambleRows).ToList();
                skippedRows += preambleRows;

                ApplyCellEdits(dataRows, request.CellEdits);

                var filtered = FilterBySelectedRows(dataRows, request.SelectedRowIndices);

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
                    dataStartRowNumber: skippedRows + 1,
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

    private static List<List<string>> FilterBySelectedRows(
        List<List<string>> dataRows,
        IReadOnlyList<int> selectedRowIndices)
    {
        if (selectedRowIndices.Count == 0)
        {
            return dataRows;
        }

        var selected = new System.Collections.Generic.HashSet<int>(selectedRowIndices);
        return dataRows
            .Select((row, index) => (row, index))
            .Where(t => selected.Contains(t.index))
            .Select(t => t.row)
            .ToList();
    }
}