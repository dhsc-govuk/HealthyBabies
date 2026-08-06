using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Organisations.Exceptions;
using Application.Organisations.Helpers;
using Domain.Locations;
using Domain.Organisations;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Locations.Commands;

public record BulkUploadLocationCellEdit(int RowNumber, string QuestionCode, string Value);

public record ProcessStagedBulkUploadLocationsCommand : IRequest<Either<LocationException, BulkUploadLocationsResultDto>>
{
    public required Guid StagingId { get; init; }
    public required OrganisationId OrganisationId { get; init; }
    public IReadOnlyList<int> SelectedRowNumbers { get; init; } = [];
    public IReadOnlyList<BulkUploadLocationCellEdit> CellEdits { get; init; } = [];
}

public record BulkUploadLocationsResultDto(
    bool Success,
    int TotalRows,
    int SuccessCount,
    int ErrorCount,
    IReadOnlyList<BulkUploadLocationRowResultDto> Results);

public record BulkUploadLocationRowResultDto(
    int RowNumber,
    string? SiteName,
    bool IsSuccess,
    string? ErrorMessage = null,
    Guid? LocationId = null,
    bool IsNew = false);

public class ProcessStagedBulkUploadLocationsCommandHandler(
    PermissionsService permissionsService,
    ILocationRepository locationRepository,
    ISiteFormQuestionQueries siteFormQuestionQueries,
    IEnumerable<IBulkUploadFileParser> fileParsers,
    IBlobService blobService)
    : IRequestHandler<ProcessStagedBulkUploadLocationsCommand, Either<LocationException, BulkUploadLocationsResultDto>>
{
    private const string ExpiredMessage = "Upload session expired or not found, please re-upload";

    public async Task<Either<LocationException, BulkUploadLocationsResultDto>> Handle(
        ProcessStagedBulkUploadLocationsCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetLocationPermissions();
        return await permissions.MatchAsync(
            async p => await Process(request, cancellationToken),
            e => new LocationArgumentException(LocationId.Empty(), e.Message));
    }

    private async Task<Either<LocationException, BulkUploadLocationsResultDto>> Process(
        ProcessStagedBulkUploadLocationsCommand request,
        CancellationToken cancellationToken)
    {
        var namesResult = await blobService.GetNamesByPrefix(
            ValidateBulkUploadLocationsCommandHandler.StagingContainer,
            request.StagingId.ToString(),
            cancellationToken);

        var matchingBlobNames = namesResult.Match(names => names, _ => (IReadOnlyList<string>)Array.Empty<string>());
        var blobName = matchingBlobNames.FirstOrDefault();
        if (blobName == null)
        {
            return new LocationArgumentException(LocationId.Empty(), ExpiredMessage);
        }

        var readResult = await blobService.GetReadStream(
            ValidateBulkUploadLocationsCommandHandler.StagingContainer,
            blobName,
            cancellationToken);

        return await readResult.MatchAsync(
            async stream => await ProcessStagedFile(stream, blobName, request, cancellationToken),
            ex => new LocationArgumentException(LocationId.Empty(), ExpiredMessage));
    }

    private async Task<Either<LocationException, BulkUploadLocationsResultDto>> ProcessStagedFile(
        Stream stagedStream,
        string blobName,
        ProcessStagedBulkUploadLocationsCommand request,
        CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        await using (stagedStream)
        {
            await stagedStream.CopyToAsync(buffer, cancellationToken);
        }

        buffer.Position = 0;
        var formFile = new FormFile(buffer, 0, buffer.Length, "file", blobName);

        var parser = fileParsers.FirstOrDefault(p => p.CanParse(formFile));
        if (parser == null)
        {
            return new LocationArgumentException(LocationId.Empty(), "Unsupported staged file format");
        }

        var parseResult = await parser.ParseAsync(formFile, cancellationToken);
        if (!parseResult.IsSuccess)
        {
            return new LocationArgumentException(LocationId.Empty(), parseResult.ErrorMessage ?? "Failed to parse staged file");
        }

        ApplyCellEdits(parseResult.Records, request.CellEdits);
        var records = FilterBySelectedRows(parseResult.Records, request.SelectedRowNumbers);

        var questions = await siteFormQuestionQueries.GetAllActive(cancellationToken);
        var results = new List<BulkUploadLocationRowResultDto>();
        var successCount = 0;
        var errorCount = 0;
        var processedNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            var rowNumber = int.Parse(record.GetValueOrDefault("_RowNumber", "0"));
            var siteName = record.GetValueOrDefault(LocationBulkUploadRecordHelper.PredefinedNameCode, string.Empty);

            var errors = LocationBulkUploadRecordHelper.ValidateRecord(record, questions);
            if (errors.Count > 0 || string.IsNullOrWhiteSpace(siteName))
            {
                results.Add(new BulkUploadLocationRowResultDto(rowNumber, siteName, false, "Row has validation errors"));
                errorCount++;
                continue;
            }

            if (!processedNames.Add(siteName))
            {
                results.Add(new BulkUploadLocationRowResultDto(rowNumber, siteName, false, "Duplicate site name in upload"));
                errorCount++;
                continue;
            }

            var existingLocation = await locationRepository.FindByNameAsync(siteName, request.OrganisationId, cancellationToken);
            var isNew = existingLocation.Match(_ => false, () => true);

            var savedLocationId = await existingLocation.Match(
                async location =>
                {
                    LocationBulkUploadRecordHelper.UpdateExistingLocation(location, record, questions);
                    await locationRepository.UpdateAsync(location, cancellationToken);
                    return location.Id.Value;
                },
                async () =>
                {
                    var location = LocationBulkUploadRecordHelper.BuildNewLocation(record, request.OrganisationId, questions);
                    await locationRepository.AddAsync(location, cancellationToken);
                    return location.Id.Value;
                });

            results.Add(new BulkUploadLocationRowResultDto(rowNumber, siteName, true, LocationId: savedLocationId, IsNew: isNew));
            successCount++;
        }

        await blobService.RemoveFile(ValidateBulkUploadLocationsCommandHandler.StagingContainer, blobName, cancellationToken);

        return new BulkUploadLocationsResultDto(errorCount == 0, records.Count, successCount, errorCount, results);
    }

    private static void ApplyCellEdits(
        List<Dictionary<string, string>> records,
        IReadOnlyList<BulkUploadLocationCellEdit> cellEdits)
    {
        foreach (var edit in cellEdits)
        {
            var record = records.FirstOrDefault(r => r.GetValueOrDefault("_RowNumber", string.Empty) == edit.RowNumber.ToString());
            if (record != null)
            {
                record[edit.QuestionCode] = edit.Value;
            }
        }
    }

    private static List<Dictionary<string, string>> FilterBySelectedRows(
        List<Dictionary<string, string>> records,
        IReadOnlyList<int> selectedRowNumbers)
    {
        if (selectedRowNumbers.Count == 0)
        {
            return records;
        }

        var selectedSet = selectedRowNumbers.ToHashSet();
        return records
            .Where(r => int.TryParse(r.GetValueOrDefault("_RowNumber", "0"), out var rowNumber) && selectedSet.Contains(rowNumber))
            .ToList();
    }
}