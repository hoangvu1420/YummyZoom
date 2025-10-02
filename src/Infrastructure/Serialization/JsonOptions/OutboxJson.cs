using System.Text.Json;
using YummyZoom.Infrastructure.Serialization.Converters;

namespace YummyZoom.Infrastructure.Serialization.JsonOptions;

public static class OutboxJson
{
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var o = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        o.Converters.Add(new AggregateRootIdJsonConverterFactory());
        return o;
    }
}
