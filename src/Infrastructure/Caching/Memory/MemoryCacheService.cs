using Microsoft.Extensions.Caching.Memory;
using YummyZoom.Application.Common.Caching;

namespace YummyZoom.Infrastructure.Caching.Memory;

public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(key, out var value) && value is T t)
        {
            return Task.FromResult<T?>(t);
        }
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, CachePolicy policy, CancellationToken ct = default)
    {
        var options = new MemoryCacheEntryOptions();
        if (policy.AbsoluteExpirationRelativeToNow.HasValue)
            options.SetAbsoluteExpiration(policy.AbsoluteExpirationRelativeToNow.Value);
        if (policy.SlidingExpiration.HasValue)
            options.SetSlidingExpiration(policy.SlidingExpiration.Value);
        _cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task InvalidateByTagAsync(string tag, CancellationToken ct = default)
    {
        // Tags not supported for memory cache in v1
        return Task.CompletedTask;
    }
}

