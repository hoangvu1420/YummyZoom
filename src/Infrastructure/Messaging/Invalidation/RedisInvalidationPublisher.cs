using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using YummyZoom.Application.Common.Caching;

namespace YummyZoom.Infrastructure.Messaging.Invalidation;

public sealed class RedisInvalidationPublisher : ICacheInvalidationPublisher
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisInvalidationPublisher> _logger;
    private const string DefaultChannel = "cache:invalidate";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RedisInvalidationPublisher(IConnectionMultiplexer redis, ILogger<RedisInvalidationPublisher> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task PublishAsync(CacheInvalidationMessage message, CancellationToken ct = default)
    {
        try
        {
            var payload = JsonSerializer.Serialize(message, JsonOptions);
            var sub = _redis.GetSubscriber();
            await sub.PublishAsync(RedisChannel.Literal(DefaultChannel), payload);
            _logger.LogInformation("Published cache invalidation: keys={KeysCount}, tags={TagsCount}", message.Keys?.Count ?? 0, message.Tags?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish cache invalidation message");
        }
    }
}
