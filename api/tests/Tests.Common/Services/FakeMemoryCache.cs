using Application.Common.Interfaces;
using Domain.Common;
using LanguageExt;

namespace Tests.Common.Services;

public class FakeMemoryCache : IInMemoryCache
{
    private readonly Dictionary<string, CacheEntry> _cache;

    public FakeMemoryCache()
    {
        _cache = new Dictionary<string, CacheEntry>();
    }

    public Option<T> GetItem<T>(string key) where T : class
    {
        Guard.NotNullOrEmpty(key, nameof(key), "Empty cache key received");

        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.ExpirationTime > DateTime.UtcNow)
            {
                return entry.Item is T item ? item : Option<T>.None;
            }

            _cache.Remove(key);
        }

        return Option<T>.None;
    }

    public void PutItem<T>(string key, T item, TimeSpan ttl) where T : class
    {
        Guard.NotNullOrEmpty(key, nameof(key), "Empty cache key received");
        Guard.NotNull(item, nameof(item), "Empty cache item received");

        var entry = new CacheEntry
        {
            Item = item,
            ExpirationTime = DateTime.UtcNow.Add(ttl)
        };

        _cache[key] = entry;
    }

    public void RemoveItem(string key)
    {
        _cache.Remove(key);
    }

    public void Clear()
    {
        _cache.Clear();
    }

    public int Count => _cache.Count;

    public bool ContainsKey(string key)
    {
        return _cache.ContainsKey(key);
    }

    private class CacheEntry
    {
        public required object Item { get; set; }
        public DateTime ExpirationTime { get; set; }
    }
}