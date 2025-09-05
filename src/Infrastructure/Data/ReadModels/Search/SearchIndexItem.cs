using NetTopologySuite.Geometries;
using NpgsqlTypes;

namespace YummyZoom.Infrastructure.Data.ReadModels.Search;

public sealed class SearchIndexItem
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!; // 'restaurant' | 'menu_item'
    public Guid? RestaurantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Cuisine { get; set; }
    public string[]? Tags { get; set; }
    public string[]? Keywords { get; set; }

    public bool IsOpenNow { get; set; }
    public bool IsAcceptingOrders { get; set; }
    public double? AvgRating { get; set; }
    public int ReviewCount { get; set; }
    public short? PriceBand { get; set; }

    public Point? Geo { get; set; } // geography(Point,4326)

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long SourceVersion { get; set; }
    public bool SoftDeleted { get; set; }

    // Generated columns (tsvector)
    public NpgsqlTsVector TsAll { get; private set; } = null!;
    public NpgsqlTsVector TsName { get; private set; } = null!;
    public NpgsqlTsVector TsDescr { get; private set; } = null!;
}

