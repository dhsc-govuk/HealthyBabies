using Application.Common.Interfaces;
using Domain.Users;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Mfa;

public class MfaRateLimitService : IMfaRateLimitService
{
    private readonly IMemoryCache _cache;
    private const int MaxAttemptsPerMinute = 5;
    private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(1);

    public MfaRateLimitService(IMemoryCache cache)
    {
        _cache = cache;
    }

    private static string GetCacheKey(UserId userId) => $"mfa_rate_limit_{userId.Value}";

    public bool IsRateLimited(UserId userId)
    {
        var key = GetCacheKey(userId);
        if (_cache.TryGetValue<RateLimitEntry>(key, out var entry) && entry != null)
        {
            return entry.Attempts >= MaxAttemptsPerMinute;
        }

        return false;
    }

    public void RecordAttempt(UserId userId)
    {
        var key = GetCacheKey(userId);
        var entry = _cache.GetOrCreate(key, cacheEntry =>
        {
            cacheEntry.AbsoluteExpirationRelativeToNow = WindowDuration;
            return new RateLimitEntry { Attempts = 0, WindowStart = DateTime.UtcNow };
        });

        if (entry != null)
        {
            entry.Attempts++;
            _cache.Set(key, entry, WindowDuration);
        }
    }

    public void ResetAttempts(UserId userId)
    {
        var key = GetCacheKey(userId);
        _cache.Remove(key);
    }

    public int GetRemainingAttempts(UserId userId)
    {
        var key = GetCacheKey(userId);
        if (_cache.TryGetValue<RateLimitEntry>(key, out var entry) && entry != null)
        {
            return Math.Max(0, MaxAttemptsPerMinute - entry.Attempts);
        }

        return MaxAttemptsPerMinute;
    }

    private class RateLimitEntry
    {
        public int Attempts { get; set; }
        public DateTime WindowStart { get; set; }
    }
}