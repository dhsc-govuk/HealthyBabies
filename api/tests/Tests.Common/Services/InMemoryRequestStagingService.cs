using System.Collections.Concurrent;
using Application.Common.Constants;
using Application.Common.Interfaces;
using LanguageExt;

namespace Tests.Common.Services;

public class InMemoryRequestStagingService : IRequestStagingService
{
    private sealed record StoredBody(byte[] Bytes, string ContentType, DateTime UploadedAtUtc);

    private readonly ConcurrentDictionary<Guid, StoredBody> _store = new();

    public void Clear() => _store.Clear();

    public int Count => _store.Count;

    public bool Exists(Guid stagingId) => _store.ContainsKey(stagingId);

    public async Task<Either<Exception, StagedBodyDescriptor>> StageAsync(
        Stream body,
        string contentType,
        long? contentLength,
        string? uploadedByUserId,
        CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        await body.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();

        var stagingId = Guid.NewGuid();
        var uploadedAtUtc = DateTime.UtcNow;
        _store[stagingId] = new StoredBody(bytes, contentType, uploadedAtUtc);

        return new StagedBodyDescriptor(stagingId, uploadedAtUtc.AddHours(RequestStagingConstants.MaxAgeHours));
    }

    public Task<Either<Exception, RehydratedBody>> RehydrateAsync(
        Guid stagingId,
        CancellationToken cancellationToken = default)
    {
        if (!_store.TryGetValue(stagingId, out var stored))
        {
            return Task.FromResult<Either<Exception, RehydratedBody>>(
                new FileNotFoundException($"Staged body {stagingId} not found"));
        }

        var stream = new MemoryStream(stored.Bytes, writable: false);
        return Task.FromResult<Either<Exception, RehydratedBody>>(
            new RehydratedBody(stream, stored.ContentType, stored.Bytes.Length));
    }

    public Task<Either<Exception, Unit>> RemoveAsync(
        Guid stagingId,
        CancellationToken cancellationToken = default)
    {
        _store.TryRemove(stagingId, out _);
        return Task.FromResult<Either<Exception, Unit>>(Unit.Default);
    }

    public Task<Either<Exception, int>> CleanupExpiredAsync(
        TimeSpan maxAge,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        var removed = 0;
        foreach (var (id, body) in _store.ToList())
        {
            if (body.UploadedAtUtc <= cutoff)
            {
                if (_store.TryRemove(id, out _))
                {
                    removed++;
                }
            }
        }

        return Task.FromResult<Either<Exception, int>>(removed);
    }

    public void SeedExpired(Guid stagingId, byte[] bytes, string contentType, DateTime uploadedAtUtc)
    {
        _store[stagingId] = new StoredBody(bytes, contentType, uploadedAtUtc);
    }
}