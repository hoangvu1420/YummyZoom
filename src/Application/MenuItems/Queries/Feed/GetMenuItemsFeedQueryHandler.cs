using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.MenuItems.Queries.Feed;

public sealed class GetMenuItemsFeedQueryHandler
    : IRequestHandler<GetMenuItemsFeedQuery, Result<PaginatedList<MenuItemFeedDto>>>
{
    private readonly IDbConnectionFactory _db;

    public GetMenuItemsFeedQueryHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    private sealed record Row(
        Guid ItemId,
        string Name,
        decimal PriceAmount,
        string PriceCurrency,
        string? ImageUrl,
        Guid RestaurantId,
        string RestaurantName,
        double? Rating,
        int Popularity,
        long LifetimeSoldCount);

    public async Task<Result<PaginatedList<MenuItemFeedDto>>> Handle(GetMenuItemsFeedQuery request, CancellationToken cancellationToken)
    {
        // For v1 we only support tab=popular. Validation enforces this. Keep structure for future tabs.
        var where = new List<string>
        {
            "mi.\"IsDeleted\" = FALSE",
            "mi.\"IsAvailable\" = TRUE",
            "r.\"IsDeleted\" = FALSE"
        };

        const string selectCols = """
            mi."Id"                 AS "ItemId",
            mi."Name"               AS "Name",
            mi."BasePrice_Amount"   AS "PriceAmount",
            mi."BasePrice_Currency" AS "PriceCurrency",
            mi."ImageUrl"           AS "ImageUrl",
            r."Id"                  AS "RestaurantId",
            r."Name"                AS "RestaurantName",
            rr."AverageRating"      AS "Rating",
            COALESCE(pop."qty30", 0) AS "Popularity",
            COALESCE(ms."LifetimeQuantity", 0) AS "LifetimeSoldCount"
            """;

        const string fromBase = """
            FROM "MenuItems" mi
            JOIN "Restaurants" r ON r."Id" = mi."RestaurantId"
            LEFT JOIN "RestaurantReviewSummaries" rr ON rr."RestaurantId" = r."Id"
            LEFT JOIN "MenuItemSalesSummaries" ms
                ON ms."RestaurantId" = mi."RestaurantId" AND ms."MenuItemId" = mi."Id"
            LEFT JOIN (
                SELECT oi."Snapshot_MenuItemId" AS "ItemId", CAST(SUM(oi."Quantity") AS int) AS qty30
                FROM "OrderItems" oi
                JOIN "Orders" o ON o."Id" = oi."OrderId"
                WHERE o."Status" IN ('Placed','Accepted','Preparing','ReadyForDelivery','Delivered')
                  AND o."PlacementTimestamp" >= now() - interval '30 days'
                GROUP BY oi."Snapshot_MenuItemId"
            ) pop ON pop."ItemId" = mi."Id"
            """;

        var fromWhere = $"{fromBase} WHERE {string.Join(" AND ", where)}";

        // Ordering for tab=popular
        var orderBy = "\"Popularity\" DESC NULLS LAST, COALESCE(rr.\"TotalReviews\",0) DESC, COALESCE(rr.\"AverageRating\",0) DESC, mi.\"LastModified\" DESC, mi.\"Id\" ASC";

        var (countSql, pageSql) = DapperPagination.BuildPagedSql(selectCols, fromWhere, orderBy, request.PageNumber, request.PageSize);

        using var conn = _db.CreateConnection();
        var page = await conn.QueryPageAsync<Row>(
            countSql,
            pageSql,
            new { },
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var mapped = page.Items.Select(r => new MenuItemFeedDto(
            r.ItemId,
            r.Name,
            r.PriceAmount,
            r.PriceCurrency,
            r.ImageUrl,
            r.Rating,
            r.RestaurantName,
            r.RestaurantId,
            r.LifetimeSoldCount)).ToList();

        var result = new PaginatedList<MenuItemFeedDto>(mapped, page.TotalCount, page.PageNumber, request.PageSize);
        return Result.Success(result);
    }
}
