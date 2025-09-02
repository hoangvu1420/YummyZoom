namespace YummyZoom.Infrastructure.ReadModels.Search;

public sealed class SearchIndexUpsert
{
    public Guid Id { get; init; }
    public string Type { get; init; } = default!; // 'restaurant' | 'menu_item'
    public Guid? RestaurantId { get; init; }

    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Cuisine { get; init; }
    public string[]? Tags { get; init; }
    public string[]? Keywords { get; init; }

    public bool IsOpenNow { get; init; }
    public bool IsAcceptingOrders { get; init; }
    public double? AvgRating { get; init; }
    public int ReviewCount { get; init; }
    public short? PriceBand { get; init; }

    // Provide WKT (e.g., "SRID=4326;POINT(lon lat)") or null
    public string? WktPoint { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public long SourceVersion { get; init; }
    public bool SoftDeleted { get; init; }
}

