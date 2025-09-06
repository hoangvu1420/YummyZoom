using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YummyZoom.Application.Common.Caching;

/// <summary>
/// Publishes cache invalidation messages to a bus (e.g., Redis pub/sub).
/// </summary>
public interface ICacheInvalidationPublisher
{
    Task PublishAsync(CacheInvalidationMessage message, CancellationToken ct = default);
}

public sealed class CacheInvalidationMessage
{
    public IReadOnlyCollection<string>? Keys { get; init; }
    public IReadOnlyCollection<string>? Tags { get; init; }
    public string? Reason { get; init; }
    public string? SourceEvent { get; init; }
    public DateTime OccurredUtc { get; init; } = DateTime.UtcNow;
}

