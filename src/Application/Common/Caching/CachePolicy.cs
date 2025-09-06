using System;

namespace YummyZoom.Application.Common.Caching;

/// <summary>
/// Policy controlling cache lifetimes and grouping.
/// </summary>
public sealed class CachePolicy
{
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; init; }
    public TimeSpan? SlidingExpiration { get; init; }
    public bool AllowStaleWhileRevalidate { get; init; }
    public TimeSpan? StaleTtl { get; init; }
    public string[] Tags { get; init; } = Array.Empty<string>();

    public static CachePolicy WithTtl(TimeSpan ttl, params string[] tags) => new()
    {
        AbsoluteExpirationRelativeToNow = ttl,
        Tags = tags ?? Array.Empty<string>()
    };
}

