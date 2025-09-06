using System.Threading;
using System.Threading.Tasks;

namespace YummyZoom.Application.Common.Caching;

/// <summary>
/// High-level cache operations used by application queries and services.
/// Implementations may back onto Redis (distributed) or memory cache.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    Task SetAsync<T>(string key, T value, CachePolicy policy, CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Invalidates all entries associated with a logical tag.
    /// Implementations may no-op if tag invalidation is not supported.
    /// </summary>
    Task InvalidateByTagAsync(string tag, CancellationToken ct = default);
}

