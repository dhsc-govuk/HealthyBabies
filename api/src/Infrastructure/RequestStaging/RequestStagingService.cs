using System.Globalization;
using Application.Common.Constants;
using Application.Common.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LanguageExt;

namespace Infrastructure.RequestStaging;

public class RequestStagingService(BlobServiceClient blobServiceClient) : IRequestStagingService
{
    private const string ContentTypeMetadataKey = "contentType";
    private const string ContentLengthMetadataKey = "contentLength";
    private const string UploadedByMetadataKey = "uploadedByUserId";
    private const string UploadedAtMetadataKey = "uploadedAtUtc";

    public async Task<Either<Exception, StagedBodyDescriptor>> StageAsync(
        Stream body,
        string contentType,
        long? contentLength,
        string? uploadedByUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stagingId = Guid.NewGuid();
            var uploadedAtUtc = DateTime.UtcNow;

            var containerClient = blobServiceClient.GetBlobContainerClient(RequestStagingConstants.Container);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(BlobName(stagingId));

            var metadata = new Dictionary<string, string>
            {
                [ContentTypeMetadataKey] = contentType,
                [UploadedAtMetadataKey] = uploadedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            };

            if (contentLength.HasValue)
            {
                metadata[ContentLengthMetadataKey] = contentLength.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(uploadedByUserId))
            {
                metadata[UploadedByMetadataKey] = uploadedByUserId!;
            }

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
                Metadata = metadata,
            };

            await blobClient.UploadAsync(body, uploadOptions, cancellationToken);

            return new StagedBodyDescriptor(
                stagingId,
                uploadedAtUtc.AddHours(RequestStagingConstants.MaxAgeHours));
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    public async Task<Either<Exception, RehydratedBody>> RehydrateAsync(
        Guid stagingId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(RequestStagingConstants.Container);
            var blobClient = containerClient.GetBlobClient(BlobName(stagingId));

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                return new FileNotFoundException($"Staged request body {stagingId} not found");
            }

            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            var metadata = properties.Value.Metadata;

            var contentType = metadata.TryGetValue(ContentTypeMetadataKey, out var ct)
                ? ct
                : properties.Value.ContentType ?? "application/octet-stream";

            var contentLength = metadata.TryGetValue(ContentLengthMetadataKey, out var clMeta)
                && long.TryParse(clMeta, NumberStyles.Integer, CultureInfo.InvariantCulture, out var clParsed)
                ? clParsed
                : properties.Value.ContentLength;

            var stream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
            return new RehydratedBody(stream, contentType, contentLength);
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    public async Task<Either<Exception, Unit>> RemoveAsync(
        Guid stagingId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(RequestStagingConstants.Container);
            await containerClient.DeleteBlobIfExistsAsync(BlobName(stagingId), cancellationToken: cancellationToken);
            return Unit.Default;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    public async Task<Either<Exception, int>> CleanupExpiredAsync(
        TimeSpan maxAge,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(RequestStagingConstants.Container);
            if (!await containerClient.ExistsAsync(cancellationToken))
            {
                return 0;
            }

            var cutoffUtc = DateTime.UtcNow - maxAge;
            var deleted = 0;

            await foreach (var blob in containerClient.GetBlobsAsync(
                traits: BlobTraits.Metadata,
                cancellationToken: cancellationToken))
            {
                var uploadedAtUtc = GetUploadedAtUtc(blob);
                if (uploadedAtUtc is null || uploadedAtUtc > cutoffUtc)
                {
                    continue;
                }

                await containerClient.DeleteBlobIfExistsAsync(blob.Name, cancellationToken: cancellationToken);
                deleted++;
            }

            return deleted;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static string BlobName(Guid stagingId) => $"{stagingId}.bin";

    private static DateTime? GetUploadedAtUtc(BlobItem blob)
    {
        if (blob.Metadata is not null &&
            blob.Metadata.TryGetValue(UploadedAtMetadataKey, out var raw) &&
            DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed.ToUniversalTime();
        }

        return blob.Properties.CreatedOn?.UtcDateTime;
    }
}