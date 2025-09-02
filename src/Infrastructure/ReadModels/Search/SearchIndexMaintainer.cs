using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IServices;

namespace YummyZoom.Infrastructure.ReadModels.Search;

public sealed class SearchIndexMaintainer : ISearchReadModelMaintainer
{
    private readonly IDbConnectionFactory _db;

    public SearchIndexMaintainer(IDbConnectionFactory db)
    {
        _db = db;
    }

    // --- Core upsert methods (DTO-based) ---
    public async Task UpsertAsync(SearchIndexUpsert dto, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        const string sql = """
            INSERT INTO "SearchIndexItems" (
                "Id",
                "Type",
                "RestaurantId",
                "Name",
                "Description",
                "Cuisine",
                "Tags",
                "Keywords",
                "IsOpenNow",
                "IsAcceptingOrders",
                "AvgRating",
                "ReviewCount",
                "PriceBand",
                "Geo",
                "CreatedAt",
                "UpdatedAt",
                "SourceVersion",
                "SoftDeleted"
            )
            VALUES (
                @Id,
                @Type,
                @RestaurantId,
                @Name,
                @Description,
                @Cuisine,
                @Tags,
                @Keywords,
                @IsOpenNow,
                @IsAcceptingOrders,
                @AvgRating,
                @ReviewCount,
                @PriceBand,
                CASE WHEN @WktPoint IS NULL THEN NULL ELSE ST_GeogFromText(@WktPoint) END,
                @CreatedAt,
                @UpdatedAt,
                @SourceVersion,
                @SoftDeleted
            )
            ON CONFLICT ("Id") DO UPDATE SET
                "Name"               = EXCLUDED."Name",
                "Description"        = EXCLUDED."Description",
                "Cuisine"            = EXCLUDED."Cuisine",
                "Tags"               = EXCLUDED."Tags",
                "Keywords"           = EXCLUDED."Keywords",
                "IsOpenNow"          = EXCLUDED."IsOpenNow",
                "IsAcceptingOrders"  = EXCLUDED."IsAcceptingOrders",
                "AvgRating"          = EXCLUDED."AvgRating",
                "ReviewCount"        = EXCLUDED."ReviewCount",
                "PriceBand"          = EXCLUDED."PriceBand",
                "Geo"                = EXCLUDED."Geo",
                "UpdatedAt"          = EXCLUDED."UpdatedAt",
                "SoftDeleted"        = EXCLUDED."SoftDeleted",
                "SourceVersion"      = EXCLUDED."SourceVersion"
            WHERE "SearchIndexItems"."SourceVersion" <= EXCLUDED."SourceVersion";
            """;

        await conn.ExecuteAsync(new CommandDefinition(sql, dto, cancellationToken: ct));
    }

    public async Task SoftDeleteAsync(Guid id, long sourceVersion, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        const string sql = """
            INSERT INTO "SearchIndexItems" (
                "Id",
                "Type",
                "Name",
                "SoftDeleted",
                "SourceVersion",
                "CreatedAt",
                "UpdatedAt"
            )
            VALUES (
                @Id,
                'system',
                '<deleted>',
                TRUE,
                @SourceVersion,
                now(),
                now()
            )
            ON CONFLICT ("Id") DO UPDATE SET
                "SoftDeleted"   = TRUE,
                "UpdatedAt"     = now(),
                "SourceVersion" = EXCLUDED."SourceVersion"
            WHERE "SearchIndexItems"."SourceVersion" <= EXCLUDED."SourceVersion";
            """;

        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id, SourceVersion = sourceVersion }, cancellationToken: ct));
    }

    // --- ID-driven API (mirrors FullMenu pattern) ---

    private const string getRestaurant = """
SELECT r."Id", r."Name", r."Description", r."CuisineType",
       r."IsAcceptingOrders", r."Geo_Latitude", r."Geo_Longitude",
       r."Created" AS "CreatedAt", r."LastModified" AS "UpdatedAt",
       r."IsDeleted",
       rr."AverageRating" AS "AvgRating", rr."TotalReviews" AS "ReviewCount"
FROM "Restaurants" r
LEFT JOIN "RestaurantReviewSummaries" rr ON rr."RestaurantId" = r."Id"
WHERE r."Id" = @RestaurantId
LIMIT 1;
""";

    private const string getMenuItem = """
SELECT i."Id", i."RestaurantId", i."Name", i."Description",
       i."Created" AS "CreatedAt", i."LastModified" AS "UpdatedAt",
       i."IsDeleted"
FROM "MenuItems" i
WHERE i."Id" = @MenuItemId
LIMIT 1;
""";

    private sealed record RestaurantRow(
        Guid Id,
        string Name,
        string Description,
        string CuisineType,
        bool IsAcceptingOrders,
        double? Geo_Latitude,
        double? Geo_Longitude,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        bool IsDeleted,
        double? AvgRating,
        int? ReviewCount);

    private sealed record MenuItemRow(
        Guid Id,
        Guid RestaurantId,
        string Name,
        string Description,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        bool IsDeleted);

    public async Task UpsertRestaurantByIdAsync(Guid restaurantId, long sourceVersion, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<RestaurantRow>(
            new CommandDefinition(getRestaurant, new { RestaurantId = restaurantId }, cancellationToken: ct));

        if (row is null || row.IsDeleted)
        {
            await SoftDeleteAsync(restaurantId, sourceVersion, ct);
            return;
        }

        string? wkt = (row.Geo_Longitude.HasValue && row.Geo_Latitude.HasValue)
            ? $"SRID=4326;POINT({row.Geo_Longitude.Value} {row.Geo_Latitude.Value})"
            : null;

        var dto = new SearchIndexUpsert
        {
            Id = row.Id,
            Type = "restaurant",
            RestaurantId = null,
            Name = row.Name,
            Description = row.Description,
            Cuisine = row.CuisineType,
            Tags = null,
            Keywords = null,
            IsOpenNow = false, // MVP; compute later from BusinessHours
            IsAcceptingOrders = row.IsAcceptingOrders,
            AvgRating = row.AvgRating,
            ReviewCount = row.ReviewCount ?? 0,
            PriceBand = null,
            WktPoint = wkt,
            CreatedAt = new DateTimeOffset(row.CreatedAt, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(row.UpdatedAt, TimeSpan.Zero),
            SourceVersion = sourceVersion,
            SoftDeleted = false
        };

        await UpsertAsync(dto, ct);
    }

    public async Task UpsertMenuItemByIdAsync(Guid menuItemId, long sourceVersion, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<MenuItemRow>(
            new CommandDefinition(getMenuItem, new { MenuItemId = menuItemId }, cancellationToken: ct));

        if (row is null || row.IsDeleted)
        {
            await SoftDeleteAsync(menuItemId, sourceVersion, ct);
            return;
        }

        var dto = new SearchIndexUpsert
        {
            Id = row.Id,
            Type = "menu_item",
            RestaurantId = row.RestaurantId,
            Name = row.Name,
            Description = row.Description,
            Cuisine = null,
            Tags = null,
            Keywords = null,
            IsOpenNow = false, // MVP placeholder
            IsAcceptingOrders = false, // optionally inherit from restaurant in future
            AvgRating = null,
            ReviewCount = 0,
            PriceBand = null,
            WktPoint = null,
            CreatedAt = new DateTimeOffset(row.CreatedAt, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(row.UpdatedAt, TimeSpan.Zero),
            SourceVersion = sourceVersion,
            SoftDeleted = false
        };

        await UpsertAsync(dto, ct);
    }

    public Task SoftDeleteByIdAsync(Guid id, long sourceVersion, CancellationToken ct = default)
        => SoftDeleteAsync(id, sourceVersion, ct);

    // Optional: full or restaurant-scoped rebuilds (skeletons)
    public async Task RebuildAsync(CancellationToken ct = default)
    {
        // Intentionally left as a stub for MVP; to be implemented when wiring rebuild workflows.
        await Task.CompletedTask;
    }

    public async Task RebuildRestaurantAsync(Guid restaurantId, CancellationToken ct = default)
    {
        // Intentionally left as a stub for MVP; per-restaurant rebuild using canonical tables.
        await Task.CompletedTask;
    }
}

