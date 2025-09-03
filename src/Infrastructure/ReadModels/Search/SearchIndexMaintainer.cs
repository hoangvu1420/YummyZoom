using Dapper;
using System.Text.RegularExpressions;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IServices;

namespace YummyZoom.Infrastructure.ReadModels.Search;

public sealed class SearchIndexMaintainer : ISearchReadModelMaintainer
{
    private readonly IDbConnectionFactory _db;
    private readonly TimeProvider _timeProvider;

    public SearchIndexMaintainer(IDbConnectionFactory db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
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
       r."BusinessHours" AS "BusinessHours",
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
        string BusinessHours,
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

        var currentTime = _timeProvider.GetUtcNow();
        bool isOpenNow = ComputeIsOpenNow(row.BusinessHours, currentTime);

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
            IsOpenNow = isOpenNow,
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

        // Cascade critical flags and derived fields to this restaurant's menu items to keep them in sync
        const string cascadeSql = """
UPDATE "SearchIndexItems"
   SET "IsOpenNow" = @IsOpenNow,
       "IsAcceptingOrders" = @IsAcceptingOrders,
       "Cuisine" = @Cuisine,
       "Geo" = CASE WHEN @WktPoint IS NULL THEN NULL ELSE ST_GeogFromText(@WktPoint) END,
       "UpdatedAt" = now()
 WHERE "RestaurantId" = @RestaurantId
   AND "Type" = 'menu_item'
   AND "SoftDeleted" = FALSE;
""";

        await conn.ExecuteAsync(new CommandDefinition(cascadeSql, new
        {
            RestaurantId = row.Id,
            IsOpenNow = isOpenNow,
            IsAcceptingOrders = row.IsAcceptingOrders,
            Cuisine = row.CuisineType,
            WktPoint = wkt
        }, cancellationToken: ct));
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

        // Fetch parent restaurant to inherit flags and data
        var parent = await conn.QueryFirstOrDefaultAsync<RestaurantRow>(
            new CommandDefinition(getRestaurant, new { RestaurantId = row.RestaurantId }, cancellationToken: ct));

        if (parent is null || parent.IsDeleted)
        {
            await SoftDeleteAsync(menuItemId, sourceVersion, ct);
            return;
        }

        string? wkt = (parent.Geo_Longitude.HasValue && parent.Geo_Latitude.HasValue)
            ? $"SRID=4326;POINT({parent.Geo_Longitude.Value} {parent.Geo_Latitude.Value})"
            : null;

        bool isOpenNow = ComputeIsOpenNow(parent.BusinessHours, _timeProvider.GetUtcNow());

        var dto = new SearchIndexUpsert
        {
            Id = row.Id,
            Type = "menu_item",
            RestaurantId = row.RestaurantId,
            Name = row.Name,
            Description = row.Description,
            Cuisine = parent.CuisineType,
            Tags = null,
            Keywords = null,
            IsOpenNow = isOpenNow,
            IsAcceptingOrders = parent.IsAcceptingOrders,
            AvgRating = null,
            ReviewCount = 0,
            PriceBand = null,
            WktPoint = wkt,
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
        using var conn = _db.CreateConnection();

        // Use current time as a monotonic-ish source version for rebuild purposes
        var sv = _timeProvider.GetUtcNow().Ticks;

        await UpsertRestaurantByIdAsync(restaurantId, sv, ct);

        const string getMenuItemIds = """
SELECT i."Id"
FROM "MenuItems" i
WHERE i."RestaurantId" = @RestaurantId AND i."IsDeleted" = FALSE;
""";

        var ids = await conn.QueryAsync<Guid>(new CommandDefinition(getMenuItemIds, new { RestaurantId = restaurantId }, cancellationToken: ct));

        foreach (var id in ids)
        {
            await UpsertMenuItemByIdAsync(id, sv, ct);
        }
    }

    // Very simple evaluator: supports formats like "9-5" or "09:00-17:00" (interpreted in UTC)
    private static bool ComputeIsOpenNow(string? businessHours, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(businessHours)) 
        {
            return false;
        }

        var s = businessHours.Trim();
        var m = Regex.Match(s, @"^(\d{1,2})(?::(\d{2}))?\s*-\s*(\d{1,2})(?::(\d{2}))?$");
        if (!m.Success) 
        {
            return false;
        }

        int sh = int.Parse(m.Groups[1].Value);
        int sm = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0;
        int eh = int.Parse(m.Groups[3].Value);
        int em = m.Groups[4].Success ? int.Parse(m.Groups[4].Value) : 0;

        if (sh < 0 || sh > 23 || eh < 0 || eh > 23) 
        {
            return false;
        }
        if (sm < 0 || sm > 59 || em < 0 || em > 59) 
        {
            return false;
        }

        // Fix: If end hour is smaller than start hour and no explicit minutes, assume PM for end time
        // e.g., "9-5" should be interpreted as 9:00 AM to 5:00 PM (17:00)
        if (eh <= 12 && eh < sh && !m.Groups[4].Success)
        {
            eh += 12; // Convert to PM (e.g., 5 becomes 17)
        }

        var start = new TimeSpan(sh, sm, 0);
        var end = new TimeSpan(eh, em, 0);
        if (end <= start) 
        {
            return false; // ignore overnight for MVP
        }

        var nowTime = nowUtc.TimeOfDay;
        bool result = nowTime >= start && nowTime <= end;
        return result;
    }
}
