using System.Text.Json;
using System.Text.Json.Serialization;
using YummyZoom.Infrastructure.Serialization.Converters;

namespace YummyZoom.Infrastructure.Serialization.JsonOptions;

/// <summary>
/// Provides shared JSON serialization options for domain objects.
/// Extends the existing serialization infrastructure to support EF Core JSONB mapping.
/// </summary>
public static class DomainJson
{
    /// <summary>
    /// Shared JsonSerializerOptions configured for domain object serialization.
    /// Includes automatic handling of all strongly-typed IDs via AggregateRootIdJsonConverterFactory.
    /// </summary>
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        // Add the existing converter factory that handles all AggregateRootId types
        // This automatically covers TagId, CustomizationGroupId, and other strongly-typed IDs
        options.Converters.Add(new AggregateRootIdJsonConverterFactory());
        
        // Configure enums to serialize as strings for better API descriptiveness
        options.Converters.Add(new JsonStringEnumConverter());
        
        return options;
    }
}
