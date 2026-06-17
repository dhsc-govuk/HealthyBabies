using System.Collections.Concurrent;
using Application.Common.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LanguageExt;

namespace Infrastructure.Blob;

public class BlobService(BlobServiceClient blobServiceClient) : IBlobService
{
    public Task<Either<Exception, Stream>> OpenWriteStream(
        string containerName,
        string fileName,
        CancellationToken cancellationToken = default) =>
        OpenWriteStream(containerName, fileName, new BlobWriteOptions(), cancellationToken);

    public async Task<Either<Exception, Stream>> OpenWriteStream(
        string containerName,
        string fileName,
        BlobWriteOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(fileName);

            var writeOptions = new BlobOpenWriteOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = options.ContentType,
                    ContentDisposition = options.ContentDisposition,
                },
                Metadata = options.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            };

            return await blobClient.OpenWriteAsync(true, writeOptions, cancellationToken);
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    public async Task<Either<Exception, IReadOnlyList<byte[]>>> ReadFiles(
        string containerName,
        IReadOnlyList<string> fileNames,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            var files = new ConcurrentBag<byte[]>();
            var tasks = fileNames.Select(async fileName =>
            {
                var blobClient = containerClient.GetBlobClient(fileName);
                await using var stream = await blobClient.OpenReadAsync(null, cancellationToken);

                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, cancellationToken);
                await memoryStream.FlushAsync(cancellationToken);
                files.Add(memoryStream.ToArray());
            });

            await Task.WhenAll(tasks);

            return files.ToList();
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    public async Task<Either<Exception, Stream>> GetReadStream(
        string containerName,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            return await blobClient.OpenReadAsync(null, cancellationToken);
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    public async Task<Either<Exception, IReadOnlyList<string>>> GetNamesByPrefix(
        string containerName,
        string prefix,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileNames = new List<string>();

            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobs = containerClient.GetBlobsAsync(prefix: $"{prefix}/", cancellationToken: cancellationToken);
            await foreach (var blob in blobs)
            {
                fileNames.Add(blob.Name);
            }

            return fileNames;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    public async Task<Either<Exception, Unit>> RemoveByPrefix(
        string containerName,
        string prefix,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobs = containerClient.GetBlobsAsync(prefix: $"{prefix}", cancellationToken: cancellationToken);
            await foreach (var blob in blobs)
            {
                await containerClient.DeleteBlobAsync(blobName: blob.Name, cancellationToken: cancellationToken);
            }

            return Unit.Default;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    public async Task<Either<Exception, Unit>> RemoveFile(
        string containerName,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.DeleteBlobIfExistsAsync(fileName, cancellationToken: cancellationToken);
            return Unit.Default;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}