using System.Collections.Concurrent;
using Application.Common.Interfaces;
using LanguageExt;

namespace Tests.Common.Services;

public class InMemoryBlobService : IBlobService
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new();

    public void Clear() => _store.Clear();

    private static string Key(string container, string fileName) => $"{container}/{fileName}";

    public Task<Either<Exception, Stream>> OpenWriteStream(string containerName, string fileName, CancellationToken cancellationToken = default)
        => OpenWriteStream(containerName, fileName, new BlobWriteOptions(), cancellationToken);

    public Task<Either<Exception, Stream>> OpenWriteStream(string containerName, string fileName, BlobWriteOptions options, CancellationToken cancellationToken = default)
    {
        var memory = new CapturingMemoryStream(buffer => _store[Key(containerName, fileName)] = buffer);
        return Task.FromResult<Either<Exception, Stream>>(memory);
    }

    public Task<Either<Exception, Stream>> GetReadStream(string containerName, string fileName, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(Key(containerName, fileName), out var bytes))
        {
            return Task.FromResult<Either<Exception, Stream>>(new MemoryStream(bytes, writable: false));
        }

        return Task.FromResult<Either<Exception, Stream>>(new FileNotFoundException(fileName));
    }

    public Task<Either<Exception, Unit>> RemoveByPrefix(string containerName, string prefix, CancellationToken cancellationToken = default)
    {
        var keyPrefix = $"{containerName}/{prefix}";
        foreach (var key in _store.Keys.Where(k => k.StartsWith(keyPrefix)).ToList())
        {
            _store.TryRemove(key, out _);
        }

        return Task.FromResult<Either<Exception, Unit>>(Unit.Default);
    }

    public Task<Either<Exception, Unit>> RemoveFile(string containerName, string fileName, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(Key(containerName, fileName), out _);
        return Task.FromResult<Either<Exception, Unit>>(Unit.Default);
    }

    public Task<Either<Exception, IReadOnlyList<string>>> GetNamesByPrefix(string containerName, string prefix, CancellationToken cancellationToken = default)
    {
        var keyPrefix = $"{containerName}/{prefix}";
        IReadOnlyList<string> matches = _store.Keys
            .Where(k => k.StartsWith(keyPrefix))
            .Select(k => k[(containerName.Length + 1)..])
            .ToList();
        return Task.FromResult(Either<Exception, IReadOnlyList<string>>.Right(matches));
    }

    public Task<Either<Exception, IReadOnlyList<byte[]>>> ReadFiles(string containerName, IReadOnlyList<string> fileNames, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<byte[]> result = fileNames
            .Select(name => _store.TryGetValue(Key(containerName, name), out var bytes) ? bytes : Array.Empty<byte>())
            .ToList();
        return Task.FromResult(Either<Exception, IReadOnlyList<byte[]>>.Right(result));
    }

    public bool Exists(string containerName, string fileName) => _store.ContainsKey(Key(containerName, fileName));

    private sealed class CapturingMemoryStream(Action<byte[]> onDispose) : MemoryStream
    {
        private bool captured;

        protected override void Dispose(bool disposing)
        {
            if (disposing && !captured)
            {
                captured = true;
                onDispose(ToArray());
            }

            base.Dispose(disposing);
        }
    }
}