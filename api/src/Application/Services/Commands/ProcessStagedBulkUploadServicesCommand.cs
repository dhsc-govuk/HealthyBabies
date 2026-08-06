using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Services.Dtos;
using Application.Services.Exceptions;
using Application.Services.Helpers;
using Domain.Organisations;
using Domain.Services;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Services.Commands;

public record BulkUploadServiceCellEdit(int RowNumber, string QuestionCode, string Value);

public record ProcessStagedBulkUploadServicesCommand : IRequest<Either<ServiceException, BulkUploadServicesResultDto>>
{
    public required Guid StagingId { get; init; }
    public IReadOnlyList<int> SelectedRowNumbers { get; init; } = [];
    public IReadOnlyList<BulkUploadServiceCellEdit> CellEdits { get; init; } = [];
}

public class ProcessStagedBulkUploadServicesCommandHandler(
    PermissionsService permissionsService,
    IServiceRepository serviceRepository,
    IServiceFormQuestionQueries questionQueries,
    IEnumerable<IBulkUploadFileParser> fileParsers,
    IBlobService blobService)
    : IRequestHandler<ProcessStagedBulkUploadServicesCommand, Either<ServiceException, BulkUploadServicesResultDto>>
{
    private const string ExpiredMessage = "Upload session expired or not found, please re-upload";

    public async Task<Either<ServiceException, BulkUploadServicesResultDto>> Handle(
        ProcessStagedBulkUploadServicesCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p => await Process(p.OrganisationId, request, cancellationToken),
            e => new ServiceArgumentException(ServiceId.Empty(), e.Message));
    }

    private async Task<Either<ServiceException, BulkUploadServicesResultDto>> Process(
        Option<OrganisationId> organisationIdOption,
        ProcessStagedBulkUploadServicesCommand request,
        CancellationToken cancellationToken)
    {
        return await organisationIdOption.MatchAsync(
            async organisationId => await ProcessForOrganisation(organisationId, request, cancellationToken),
            () => new ServiceArgumentException(ServiceId.Empty(), "Organisation ID is required"));
    }

    private async Task<Either<ServiceException, BulkUploadServicesResultDto>> ProcessForOrganisation(
        OrganisationId organisationId,
        ProcessStagedBulkUploadServicesCommand request,
        CancellationToken cancellationToken)
    {
        var namesResult = await blobService.GetNamesByPrefix(
            ValidateBulkUploadServicesCommandHandler.StagingContainer,
            request.StagingId.ToString(),
            cancellationToken);

        var matchingBlobNames = namesResult.Match(names => names, _ => (IReadOnlyList<string>)Array.Empty<string>());
        var blobName = matchingBlobNames.FirstOrDefault();
        if (blobName == null)
        {
            return new ServiceArgumentException(ServiceId.Empty(), ExpiredMessage);
        }

        var readResult = await blobService.GetReadStream(
            ValidateBulkUploadServicesCommandHandler.StagingContainer,
            blobName,
            cancellationToken);

        return await readResult.MatchAsync(
            async stream => await ProcessStagedFile(stream, blobName, organisationId, request, cancellationToken),
            ex => new ServiceArgumentException(ServiceId.Empty(), ExpiredMessage));
    }

    private async Task<Either<ServiceException, BulkUploadServicesResultDto>> ProcessStagedFile(
        Stream stagedStream,
        string blobName,
        OrganisationId organisationId,
        ProcessStagedBulkUploadServicesCommand request,
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
            return new ServiceArgumentException(ServiceId.Empty(), "Unsupported staged file format");
        }

        var parseResult = await parser.ParseAsync(formFile, cancellationToken);
        if (!parseResult.IsSuccess)
        {
            return new ServiceArgumentException(ServiceId.Empty(), parseResult.ErrorMessage ?? "Failed to parse staged file");
        }

        ApplyCellEdits(parseResult.Records, request.CellEdits);
        var records = FilterBySelectedRows(parseResult.Records, request.SelectedRowNumbers);

        var questions = await questionQueries.GetAllActive(cancellationToken);
        var results = new List<BulkUploadServiceRowResultDto>();
        var successCount = 0;
        var errorCount = 0;
        var processedNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            var rowNumber = int.Parse(record.GetValueOrDefault("_RowNumber", "0"));
            var serviceName = record.GetValueOrDefault(ServiceBulkUploadRecordHelper.PredefinedNameCode, string.Empty);

            var errors = ServiceBulkUploadRecordHelper.ValidateRecord(record, questions);
            if (errors.Count > 0 || string.IsNullOrWhiteSpace(serviceName))
            {
                results.Add(new BulkUploadServiceRowResultDto(rowNumber, serviceName, false, "Row has validation errors"));
                errorCount++;
                continue;
            }

            if (!processedNames.Add(serviceName))
            {
                results.Add(new BulkUploadServiceRowResultDto(rowNumber, serviceName, false, "Duplicate service name in upload"));
                errorCount++;
                continue;
            }

            var existingService = await serviceRepository.FindByNameAsync(serviceName, organisationId, cancellationToken);
            var isNew = existingService.Match(_ => false, () => true);

            var savedServiceId = await existingService.Match(
                async service =>
                {
                    service.UpdateName(serviceName);
                    ServiceBulkUploadRecordHelper.ApplyAnswers(service, record, questions);
                    service.AdvanceToStep(3);
                    service.Complete();
                    await serviceRepository.UpdateAsync(service, cancellationToken);
                    return service.Id.Value;
                },
                async () =>
                {
                    var service = Service.New(ServiceId.New(), organisationId, serviceName);
                    ServiceBulkUploadRecordHelper.ApplyAnswers(service, record, questions);
                    service.AdvanceToStep(3);
                    service.Complete();
                    await serviceRepository.AddAsync(service, cancellationToken);
                    return service.Id.Value;
                });

            results.Add(new BulkUploadServiceRowResultDto(rowNumber, serviceName, true, ServiceId: savedServiceId, IsNew: isNew));
            successCount++;
        }

        await blobService.RemoveFile(ValidateBulkUploadServicesCommandHandler.StagingContainer, blobName, cancellationToken);

        return new BulkUploadServicesResultDto(errorCount == 0, records.Count, successCount, errorCount, results);
    }

    private static void ApplyCellEdits(
        List<Dictionary<string, string>> records,
        IReadOnlyList<BulkUploadServiceCellEdit> cellEdits)
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