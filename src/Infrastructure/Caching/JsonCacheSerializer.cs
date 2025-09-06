using System.Text.Json;
using YummyZoom.Application.Common.Caching;
using YummyZoom.Infrastructure.Serialization;

namespace YummyZoom.Infrastructure.Caching;

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

