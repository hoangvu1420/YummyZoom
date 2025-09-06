namespace YummyZoom.Application.Common.Caching;

/// <summary>
/// Creates deterministic, namespaced cache keys.
/// </summary>
public interface ICacheKeyFactory
{
    /// <summary>
    /// Builds a cache key in the form of "ns:v1:part1:part2:...".
    /// Implementations decide versioning and normalization strategy.
    /// </summary>
    string Create(string @namespace, params object[] parts);
}

