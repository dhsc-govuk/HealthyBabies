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

    /// <summary>
    /// One zip containing the same per-module CSVs as <see cref="ExportSubmissionAsCsvZipAsync"/>
    /// plus a submission.json with the same content <see cref="ExportSubmissionAsJsonAsync"/>
    /// produces - the single download every "Files" card offers, replacing separate CSV/JSON links.
    /// </summary>
    Task<Either<string, byte[]>> ExportSubmissionAsZipAsync(
        Guid dataCollectionId,
        Guid localAuthorityId,
        CancellationToken cancellationToken = default);

    Task<Either<string, byte[]>> ExportModuleAsCsvAsync(
        Guid dataCollectionId,
        Guid localAuthorityId,
        Guid moduleId,
        CancellationToken cancellationToken = default);

    Task<Either<string, byte[]>> ExportConsolidatedCsvZipAsync(
        Guid dataCollectionId,
        CancellationToken cancellationToken = default);

    Task<Either<string, byte[]>> ExportConsolidatedJsonAsync(
        Guid dataCollectionId,
        CancellationToken cancellationToken = default);
}