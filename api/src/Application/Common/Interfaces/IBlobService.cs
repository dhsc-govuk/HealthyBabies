using LanguageExt;

namespace Application.Common.Interfaces;

public sealed record BlobWriteOptions(
    string? ContentType = null,
    string? ContentDisposition = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public interface IBlobService
{
    Task<Either<Exception, Stream>> OpenWriteStream(
        string containerName,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<Either<Exception, Stream>> OpenWriteStream(
        string containerName,
        string fileName,
        BlobWriteOptions options,
        CancellationToken cancellationToken = default);

    Task<Either<Exception, Stream>> GetReadStream(
        string containerName,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<Either<Exception, IReadOnlyList<string>>> GetNamesByPrefix(
        string containerName,
        string prefix,
        CancellationToken cancellationToken = default);

    Task<Either<Exception, IReadOnlyList<byte[]>>> ReadFiles(
        string containerName,
        IReadOnlyList<string> fileNames,
        CancellationToken cancellationToken = default);

    Task<Either<Exception, Unit>> RemoveByPrefix(
        string containerName,
        string prefix,
        CancellationToken cancellationToken = default);

    Task<Either<Exception, Unit>> RemoveFile(
        string containerName,
        string fileName,
        CancellationToken cancellationToken = default);
}