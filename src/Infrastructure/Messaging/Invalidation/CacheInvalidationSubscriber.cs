using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using YummyZoom.Application.Common.Caching;

namespace YummyZoom.Infrastructure.Messaging.Invalidation;

public sealed class CacheInvalidationSubscriber : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ICacheService _cache;
    private readonly ILogger<CacheInvalidationSubscriber> _logger;
    private const string DefaultChannel = "cache:invalidate";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public CacheInvalidationSubscriber(
        IConnectionMultiplexer redis,
        ICacheService cache,
        ILogger<CacheInvalidationSubscriber> logger)
    {
        _redis = redis;
        _cache = cache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sub = _redis.GetSubscriber();
        await sub.SubscribeAsync(RedisChannel.Literal(DefaultChannel), async (_, value) =>
        {
            try
            {
                var msg = JsonSerializer.Deserialize<CacheInvalidationMessage>(value!, JsonOptions);
                if (msg is null) return;

                var keysCount = 0;
                if (msg.Keys is not null)
                {
                    foreach (var k in msg.Keys)
                    {
                        await _cache.RemoveAsync(k, stoppingToken);
                        keysCount++;
                    }
                }

                var tagsCount = 0;
                if (msg.Tags is not null)
                {
                    foreach (var t in msg.Tags)
                    {
                        await _cache.InvalidateByTagAsync(t, stoppingToken);
                        tagsCount++;
                    }
                }

                _logger.LogDebug("Processed cache invalidation: removed keys={Keys}, invalidated tags={Tags}", keysCount, tagsCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process cache invalidation message");
            }
        });

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
    }
}
