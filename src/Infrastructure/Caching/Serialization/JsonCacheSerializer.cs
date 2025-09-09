using System.Text.Json;
using YummyZoom.Application.Common.Caching;
using YummyZoom.Infrastructure.Serialization;
using YummyZoom.Infrastructure.Serialization.JsonOptions;

namespace YummyZoom.Infrastructure.Caching.Serialization;

public sealed class JsonCacheSerializer : ICacheSerializer
{
    private static readonly JsonSerializerOptions Options = DomainJson.Options;

    public byte[] Serialize<T>(T value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, Options);
    }

    public T? Deserialize<T>(byte[] payload)
    {
        if (payload is null || payload.Length == 0) return default;
        return JsonSerializer.Deserialize<T>(payload, Options);
    }
}

