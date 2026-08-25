using Application.Common.Dtos;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Permissions;
using Application.Organisations.Dtos;
using Application.Organisations.Exceptions;
using Application.Organisations.Helpers;
using Domain.Locations;
using Domain.Organisations;
using Domain.SiteForms;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Locations.Commands;

public record ValidateBulkUploadLocationsCommand : IRequest<Either<LocationException, BulkUploadLocationsValidationResultDto>>
{
    public required Stream FileStream { get; init; }
    public required string FileName { get; init; }
    public required long FileLength { get; init; }
    public required OrganisationId OrganisationId { get; init; }
}

public class ValidateBulkUploadLocationsCommandHandler(
    PermissionsService permissionsService,
    ISiteFormQuestionQueries siteFormQuestionQueries,
    IEnumerable<IBulkUploadFileParser> fileParsers,
    IBlobService blobService)
    : IRequestHandler<ValidateBulkUploadLocationsCommand, Either<LocationException, BulkUploadLocationsValidationResultDto>>
{
    public const string StagingContainer = "locations-bulk-upload-staging";

    public async Task<Either<LocationException, BulkUploadLocationsValidationResultDto>> Handle(
        ValidateBulkUploadLocationsCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetLocationPermissions();
        return await permissions.MatchAsync(
            async p => await ProcessUpload(request, cancellationToken),
            e => new LocationArgumentException(LocationId.Empty(), e.Message));
    }

    private async Task<Either<LocationException, BulkUploadLocationsValidationResultDto>> ProcessUpload(
        ValidateBulkUploadLocationsCommand request,
        CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        await request.FileStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var formFile = new FormFile(buffer, 0, buffer.Length, "file", request.FileName);

        var parser = fileParsers.FirstOrDefault(p => p.CanParse(formFile));
        if (parser == null)
        {
            return new LocationArgumentException(
                LocationId.Empty(),
                "Unsupported file format. Please upload a CSV (.csv) or Excel (.xlsx) file.");
        }

        var parseResult = await parser.ParseAsync(formFile, cancellationToken);
        if (!parseResult.IsSuccess)
        {
            return new LocationArgumentException(LocationId.Empty(), parseResult.ErrorMessage ?? "Failed to parse file");
        }

        if (parseResult.Records.Count == 0)
        {
            return new LocationArgumentException(LocationId.Empty(), "The file contains no data rows");
        }

        var questions = await siteFormQuestionQueries.GetAllActive(cancellationToken);
        var (rowValidations, validCount, invalidCount) = BuildRowValidations(parseResult.Records, questions);
        var fieldMetadata = BuildFieldMetadata(questions);

        buffer.Position = 0;
        var stageResult = await StageUpload(buffer, request, cancellationToken);

        return stageResult.Match<Either<LocationException, BulkUploadLocationsValidationResultDto>>(
            stagingId => new BulkUploadLocationsValidationResultDto(
                invalidCount == 0,
                parseResult.Records.Count,
                validCount,
                invalidCount,
                rowValidations,
                fieldMetadata,
                stagingId),
            error => new LocationArgumentException(LocationId.Empty(), error));
    }

    private static (List<BulkUploadRowValidationDto> RowValidations, int ValidCount, int InvalidCount) BuildRowValidations(
        List<Dictionary<string, string>> records,
        IReadOnlyList<SiteFormQuestion> questions)
    {
        var rowValidations = new List<BulkUploadRowValidationDto>();
        var validCount = 0;
        var invalidCount = 0;
        var seenNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            var rowNumber = int.Parse(record.GetValueOrDefault("_RowNumber", "0"));
            var siteName = record.GetValueOrDefault(LocationBulkUploadRecordHelper.PredefinedNameCode, string.Empty);

            var errors = LocationBulkUploadRecordHelper.ValidateRecord(record, questions)
                .Select(ToFieldError(questions))
                .ToList();

            if (string.IsNullOrWhiteSpace(siteName))
            {
                errors.Add(new BulkUploadFieldErrorDto(LocationBulkUploadRecordHelper.PredefinedNameCode, "Name of the location", "Site name is required"));
            }
            else if (!seenNames.Add(siteName))
            {
                errors.Add(new BulkUploadFieldErrorDto(LocationBulkUploadRecordHelper.PredefinedNameCode, "Name of the location", $"Duplicate site name '{siteName}' found in upload"));
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

            rowValidations.Add(new BulkUploadRowValidationDto(rowNumber, siteName, isValid, errors));
        }

        return (rowValidations, validCount, invalidCount);
    }

    private static Func<BulkUploadRowValidationError, BulkUploadFieldErrorDto> ToFieldError(
        IReadOnlyList<SiteFormQuestion> questions) =>
        error => new BulkUploadFieldErrorDto(
            error.QuestionCode,
            questions.FirstOrDefault(q => q.Code == error.QuestionCode)?.Label ?? error.QuestionCode,
            error.Message);

    private static List<BulkUploadLocationFieldMetadataDto> BuildFieldMetadata(IReadOnlyList<SiteFormQuestion> questions) =>
        questions.Select(q => new BulkUploadLocationFieldMetadataDto(
            q.Code,
            q.QuestionType.ToString(),
            q.IsRequired,
            q.Options.OrderBy(o => o.DisplayOrder).Select(o => new BulkUploadFieldOptionDto(o.Value, o.Label)).ToList(),
            q.ConditionalQuestionCode,
            q.ConditionalValue)).ToList();

    private async Task<Either<string, Guid>> StageUpload(
        MemoryStream buffer,
        ValidateBulkUploadLocationsCommand request,
        CancellationToken cancellationToken)
    {
        var stagingId = Guid.NewGuid();
        var extension = Path.GetExtension(request.FileName);

        var uploadOptions = new BlobWriteOptions(
            ContentType: extension.Equals(".csv", StringComparison.OrdinalIgnoreCase) ? "text/csv" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Metadata: new Dictionary<string, string>
            {
                ["organisationId"] = request.OrganisationId.Value.ToString(),
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