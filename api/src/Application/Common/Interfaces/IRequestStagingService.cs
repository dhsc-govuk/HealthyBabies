using LanguageExt;

namespace Application.Common.Interfaces;

public sealed record StagedBodyDescriptor(Guid StagingId, DateTime ExpiresAtUtc);

public sealed record RehydratedBody(Stream Body, string ContentType, long ContentLength);

public interface IRequestStagingService
{
    Task<Either<Exception, StagedBodyDescriptor>> StageAsync(
        Stream body,
        string contentType,
        long? contentLength,
        string? uploadedByUserId,
        CancellationToken cancellationToken = default);

    Task<Either<Exception, RehydratedBody>> RehydrateAsync(
        Guid stagingId,
        CancellationToken cancellationToken = default);

    Task<Either<Exception, Unit>> RemoveAsync(
        Guid stagingId,
        CancellationToken cancellationToken = default);

    Task<Either<Exception, int>> CleanupExpiredAsync(
        TimeSpan maxAge,
        CancellationToken cancellationToken = default);
}