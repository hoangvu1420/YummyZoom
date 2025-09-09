using YummyZoom.Application.Common.Caching;

namespace YummyZoom.Infrastructure.Messaging.Invalidation;

public sealed class NoOpInvalidationPublisher : ICacheInvalidationPublisher
{
    public Task PublishAsync(CacheInvalidationMessage message, CancellationToken ct = default)
        => Task.CompletedTask;
}

