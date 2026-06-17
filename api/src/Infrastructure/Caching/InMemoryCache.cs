using Application.Common.Interfaces;
using Domain.Common;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Caching;

public class InMemoryCache(IMemoryCache cache) : IInMemoryCache
{
    public Option<T> GetItem<T>(string key)
        where T : class
    {
        Guard.NotNullOrEmpty(key, nameof(key), "Empty cache key received");
        return cache.TryGetValue(key, out T? cachedItem) ? cachedItem : Option<T>.None;
    }

    public void PutItem<T>(string key, T item, TimeSpan ttl)
        where T : class
    {
        Guard.NotNullOrEmpty(key, nameof(key), "Empty cache key received");
        Guard.NotNull(item, nameof(item), "Empty cache item received");
        cache.Set(key, item, ttl);
    }
}