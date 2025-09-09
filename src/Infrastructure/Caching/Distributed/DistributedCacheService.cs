using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using YummyZoom.Application.Common.Caching;

namespace YummyZoom.Infrastructure.Caching.Distributed;

public sealed class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ICacheSerializer _serializer;
    private readonly IConnectionMultiplexer? _redis;

    private const string TagSetPrefix = "cache:tag:";
    private static readonly TimeSpan TagTtlSkew = TimeSpan.FromSeconds(60);

    public DistributedCacheService(IDistributedCache cache, ICacheSerializer serializer, IConnectionMultiplexer? redis = null)
    {
        _cache = cache;
        _serializer = serializer;
        _redis = redis;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(key, ct);
        if (bytes is null) return default;
        return _serializer.Deserialize<T>(bytes);
    }

    public async Task SetAsync<T>(string key, T value, CachePolicy policy, CancellationToken ct = default)
    {
        var bytes = _serializer.Serialize(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = policy.AbsoluteExpirationRelativeToNow,
            SlidingExpiration = policy.SlidingExpiration,
        };
        await _cache.SetAsync(key, bytes, options, ct);

        // Tag indexing in Redis (if multiplexer available)
        if (_redis is not null && policy.Tags is { Length: > 0 })
        {
            var db = _redis.GetDatabase();
            var expiry = policy.AbsoluteExpirationRelativeToNow?.Add(TagTtlSkew);
            foreach (var tag in policy.Tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                var tagKey = TagSetPrefix + tag;
                // Add key to the tag set and apply TTL to the set (best-effort)
                _ = db.SetAddAsync(tagKey, key);
                if (expiry.HasValue)
                {
                    _ = db.KeyExpireAsync(tagKey, expiry);
                }
            }
        }
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        return _cache.RemoveAsync(key, ct);
    }

    public async Task InvalidateByTagAsync(string tag, CancellationToken ct = default)
    {
        if (_redis is null || string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        var db = _redis.GetDatabase();
        var tagKey = TagSetPrefix + tag;

        // Fetch all members of the tag set and delete them, then delete the set
        var members = await db.SetMembersAsync(tagKey);
        if (members.Length > 0)
        {
            // Remove each cached key via IDistributedCache to respect its provider storage
            foreach (var m in members)
            {
                if (ct.IsCancellationRequested) break;
                var memberKey = m.ToString();
                if (!string.IsNullOrEmpty(memberKey))
                {
                    await _cache.RemoveAsync(memberKey, ct);
                }
            }
        }

        // Remove the tag set itself
        await db.KeyDeleteAsync(tagKey);
    }
}
