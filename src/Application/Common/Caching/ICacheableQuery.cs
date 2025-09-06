namespace YummyZoom.Application.Common.Caching;

/// <summary>
/// Marker for MediatR queries that participate in cache-aside behavior.
/// </summary>
/// <typeparam name="TResponse">Query response type.</typeparam>
public interface ICacheableQuery<TResponse>
{
    string CacheKey { get; }
    CachePolicy Policy { get; }
}

