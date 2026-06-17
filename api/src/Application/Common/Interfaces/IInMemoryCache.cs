using LanguageExt;

namespace Application.Common.Interfaces;

public interface IInMemoryCache
{
    Option<T> GetItem<T>(string key)
        where T : class;
    void PutItem<T>(string key, T item, TimeSpan ttl)
        where T : class;
}