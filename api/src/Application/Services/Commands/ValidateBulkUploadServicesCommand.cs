using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Permissions;
using Application.Services.Dtos;
using Application.Services.Exceptions;
using Application.Services.Helpers;
using Domain.ServiceForms;
using Domain.Services;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Services.Commands;

public record ValidateBulkUploadServicesCommand : IRequest<Either<ServiceException, BulkUploadServicesValidationResultDto>>
{
    public required Stream FileStream { get; init; }
    public required string FileName { get; init; }
    public required long FileLength { get; init; }
}

public class ValidateBulkUploadServicesCommandHandler(
    PermissionsService permissionsService,
    IServiceFormQuestionQueries questionQueries,
    IEnumerable<IBulkUploadFileParser> fileParsers,
    IBlobService blobService)
    : IRequestHandler<ValidateBulkUploadServicesCommand, Either<ServiceException, BulkUploadServicesValidationResultDto>>
{
    public const string StagingContainer = "services-bulk-upload-staging";

    public async Task<Either<ServiceException, BulkUploadServicesValidationResultDto>> Handle(
        ValidateBulkUploadServicesCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p => await ProcessUpload(request, cancellationToken),
            e => new ServiceArgumentException(ServiceId.Empty(), e.Message));
    }

    private async Task<Either<ServiceException, BulkUploadServicesValidationResultDto>> ProcessUpload(
        ValidateBulkUploadServicesCommand request,
        CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        await request.FileStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var formFile = new FormFile(buffer, 0, buffer.Length, "file", request.FileName);

        var parser = fileParsers.FirstOrDefault(p => p.CanParse(formFile));
        if (parser == null)
        {
            return new ServiceArgumentException(
                ServiceId.Empty(),
                "Unsupported file format. Please upload a CSV (.csv) or Excel (.xlsx) file.");
        }

        var parseResult = await parser.ParseAsync(formFile, cancellationToken);
        if (!parseResult.IsSuccess)
        {
            return new ServiceArgumentException(ServiceId.Empty(), parseResult.ErrorMessage ?? "Failed to parse file");
        }

        if (parseResult.Records.Count == 0)
        {
            return new ServiceArgumentException(ServiceId.Empty(), "The file contains no data rows");
        }

        var questions = await questionQueries.GetAllActive(cancellationToken);
        var (rowValidations, validCount, invalidCount) = BuildRowValidations(parseResult.Records, questions);
        var fieldMetadata = BuildFieldMetadata(questions);

        buffer.Position = 0;
        var stageResult = await StageUpload(buffer, request, cancellationToken);

        return stageResult.Match<Either<ServiceException, BulkUploadServicesValidationResultDto>>(
            stagingId => new BulkUploadServicesValidationResultDto(
                invalidCount == 0,
                parseResult.Records.Count,
                validCount,
                invalidCount,
                rowValidations,
                fieldMetadata,
                stagingId),
            error => new ServiceArgumentException(ServiceId.Empty(), error));
    }

    private static (List<BulkUploadServiceRowValidationDto> RowValidations, int ValidCount, int InvalidCount) BuildRowValidations(
        List<Dictionary<string, string>> records,
        IReadOnlyList<ServiceFormQuestion> questions)
    {
        var rowValidations = new List<BulkUploadServiceRowValidationDto>();
        var validCount = 0;
        var invalidCount = 0;
        var seenNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            var rowNumber = int.Parse(record.GetValueOrDefault("_RowNumber", "0"));
            var serviceName = record.GetValueOrDefault(ServiceBulkUploadRecordHelper.PredefinedNameCode, string.Empty);

            var errors = ServiceBulkUploadRecordHelper.ValidateRecord(record, questions)
                .Select(ToFieldError(questions))
                .ToList();

            if (string.IsNullOrWhiteSpace(serviceName))
            {
                errors.Add(new BulkUploadServiceFieldErrorDto(ServiceBulkUploadRecordHelper.PredefinedNameCode, "Name of the service", "Service name is required"));
            }
            else if (!seenNames.Add(serviceName))
            {
                errors.Add(new BulkUploadServiceFieldErrorDto(ServiceBulkUploadRecordHelper.PredefinedNameCode, "Name of the service", $"Duplicate service name '{serviceName}' found in upload"));
            }

            var isValid = errors.Count == 0;
            if (isValid)
            {
                validCount++;
            }
            else
            {
                invalidCount++;
            }

            rowValidations.Add(new BulkUploadServiceRowValidationDto(rowNumber, serviceName, isValid, errors));
        }

        return (rowValidations, validCount, invalidCount);
    }

    private static Func<BulkUploadRowValidationError, BulkUploadServiceFieldErrorDto> ToFieldError(
        IReadOnlyList<ServiceFormQuestion> questions) =>
        error => new BulkUploadServiceFieldErrorDto(
            error.QuestionCode,
            questions.FirstOrDefault(q => q.Code == error.QuestionCode)?.Label ?? error.QuestionCode,
            error.Message);

    private static List<BulkUploadServiceFieldMetadataDto> BuildFieldMetadata(IReadOnlyList<ServiceFormQuestion> questions) =>
        questions.Select(q => new BulkUploadServiceFieldMetadataDto(
            q.Code,
            q.QuestionType.ToString(),
            q.IsRequired,
            q.Options.OrderBy(o => o.DisplayOrder).Select(o => new BulkUploadServiceFieldOptionDto(o.Value, o.Label)).ToList(),
            q.ConditionalQuestionCode,
            q.ConditionalValue)).ToList();

    private async Task<Either<string, Guid>> StageUpload(
        MemoryStream buffer,
        ValidateBulkUploadServicesCommand request,
        CancellationToken cancellationToken)
    {
        var stagingId = Guid.NewGuid();
        var extension = Path.GetExtension(request.FileName);

        var uploadOptions = new BlobWriteOptions(
            ContentType: extension.Equals(".csv", StringComparison.OrdinalIgnoreCase) ? "text/csv" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Metadata: new Dictionary<string, string>
            {
                ["uploadedAtUtc"] = DateTime.UtcNow.ToString("O"),
                ["originalFileName"] = request.FileName,
            });

        // Nested under a "{stagingId}/" virtual folder rather than a flat "{stagingId}{extension}" name -
        // IBlobService.GetNamesByPrefix (used to look this back up without knowing the extension) matches
        // on "{prefix}/", so a flat name would never be found again at confirm time.
        var streamResult = await blobService.OpenWriteStream(StagingContainer, $"{stagingId}/upload{extension}", uploadOptions, cancellationToken);

        return await streamResult.MatchAsync<Either<string, Guid>>(
            async writeStream =>
            {
                await using (writeStream)
                {
                    await buffer.CopyToAsync(writeStream, cancellationToken);
                }

                return stagingId;
            },
            ex => $"Failed to stage upload: {ex.Message}");
    }
}