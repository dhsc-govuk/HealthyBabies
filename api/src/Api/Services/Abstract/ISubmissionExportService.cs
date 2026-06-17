using LanguageExt;

namespace Api.Services.Abstract;

public interface ISubmissionExportService
{
    Task<Either<string, byte[]>> ExportSubmissionAsCsvZipAsync(
        Guid dataCollectionId,
        Guid localAuthorityId,
        CancellationToken cancellationToken = default);

    Task<Either<string, byte[]>> ExportSubmissionAsJsonAsync(
        Guid dataCollectionId,
        Guid localAuthorityId,
        CancellationToken cancellationToken = default);

    Task<Either<string, byte[]>> ExportConsolidatedCsvZipAsync(
        Guid dataCollectionId,
        CancellationToken cancellationToken = default);

    Task<Either<string, byte[]>> ExportConsolidatedJsonAsync(
        Guid dataCollectionId,
        CancellationToken cancellationToken = default);
}