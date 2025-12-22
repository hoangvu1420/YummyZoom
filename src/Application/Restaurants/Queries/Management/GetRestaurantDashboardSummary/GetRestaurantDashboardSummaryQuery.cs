using Dapper;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Restaurants.Queries.Management.GetRestaurantDashboardSummary;

/// <summary>
/// Aggregated snapshot used by restaurant staff dashboards.
/// </summary>
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record GetRestaurantDashboardSummaryQuery(Guid RestaurantId, int? TopItemsLimit = null)
    : IRequest<Result<RestaurantDashboardSummaryDto>>, IRestaurantQuery
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantQuery.RestaurantId =>
        Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class GetRestaurantDashboardSummaryQueryHandler
    : IRequestHandler<GetRestaurantDashboardSummaryQuery, Result<RestaurantDashboardSummaryDto>>
{
    private readonly IDbConnectionFactory _db;

    public GetRestaurantDashboardSummaryQueryHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<Result<RestaurantDashboardSummaryDto>> Handle(
        GetRestaurantDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var topItemsLimit = request.TopItemsLimit.GetValueOrDefault(5);
        topItemsLimit = Math.Clamp(topItemsLimit, 1, 25);

        const string summarySql = """
            SELECT
                r."Id" AS RestaurantId,
                r."Name" AS RestaurantName,
                r."LogoUrl" AS LogoUrl,
                r."IsVerified" AS IsVerified,
                r."IsAcceptingOrders" AS IsAcceptingOrders,
                COUNT(o.*) FILTER (WHERE o."Status" = 'Placed')::int AS NewOrders,
                COUNT(o.*) FILTER (WHERE o."Status" IN ('Placed','Accepted','Preparing','ReadyForDelivery'))::int AS ActiveOrders,
                MAX(o."PlacementTimestamp") AS LastOrderAtUtc,
                COUNT(o.*) FILTER (WHERE o."PlacementTimestamp" >= NOW() - INTERVAL '7 days')::int AS OrdersLast7Days,
                COUNT(o.*) FILTER (WHERE o."PlacementTimestamp" >= NOW() - INTERVAL '30 days')::int AS OrdersLast30Days,
                COALESCE(SUM(CASE WHEN o."Status" = 'Delivered'
                                  AND o."PlacementTimestamp" >= NOW() - INTERVAL '7 days'
                                  THEN o."TotalAmount_Amount" ELSE 0 END), 0)::decimal(18,2) AS RevenueLast7Days,
                COALESCE(SUM(CASE WHEN o."Status" = 'Delivered'
                                  AND o."PlacementTimestamp" >= NOW() - INTERVAL '30 days'
                                  THEN o."TotalAmount_Amount" ELSE 0 END), 0)::decimal(18,2) AS RevenueLast30Days
            FROM "Restaurants" r
            LEFT JOIN "Orders" o ON o."RestaurantId" = r."Id"
            WHERE r."Id" = @RestaurantId AND r."IsDeleted" = FALSE
            GROUP BY r."Id", r."Name", r."LogoUrl", r."IsVerified", r."IsAcceptingOrders";
            """;

        var summary = await conn.QuerySingleOrDefaultAsync<DashboardSummaryRow>(
            new CommandDefinition(summarySql, new { request.RestaurantId }, cancellationToken: cancellationToken));

        if (summary is null)
        {
            return Result.Failure<RestaurantDashboardSummaryDto>(RestaurantDashboardErrors.NotFound);
        }

        const string reviewSql = """
            SELECT
                s."AverageRating" AS AverageRating,
                s."TotalReviews" AS TotalReviews,
                s."LastReviewAtUtc" AS LastReviewAtUtc,
                s."UpdatedAtUtc" AS UpdatedAtUtc
            FROM "RestaurantReviewSummaries" s
            WHERE s."RestaurantId" = @RestaurantId
            LIMIT 1;
            """;

        var review = await conn.QuerySingleOrDefaultAsync<ReviewSummaryRow>(
            new CommandDefinition(reviewSql, new { request.RestaurantId }, cancellationToken: cancellationToken));

        const string balanceSql = """
            SELECT
                COALESCE(r."CurrentBalance_Amount", 0)::decimal(18,2) AS CurrentBalance,
                COALESCE(r."CurrentBalance_Currency", 'USD') AS Currency
            FROM "RestaurantAccounts" r
            WHERE r."RestaurantId" = @RestaurantId
            LIMIT 1;
            """;

        var balance = await conn.QuerySingleOrDefaultAsync<BalanceRow>(
            new CommandDefinition(balanceSql, new { request.RestaurantId }, cancellationToken: cancellationToken));

        const string topItemsSql = """
            SELECT
                ms."MenuItemId" AS MenuItemId,
                mi."Name" AS Name,
                ms."Rolling7DayQuantity" AS Rolling7DayQuantity,
                ms."Rolling30DayQuantity" AS Rolling30DayQuantity,
                ms."LastUpdatedAt" AS LastUpdatedAt
            FROM "MenuItemSalesSummaries" ms
            JOIN "MenuItems" mi ON mi."Id" = ms."MenuItemId"
            WHERE ms."RestaurantId" = @RestaurantId AND mi."IsDeleted" = FALSE
            ORDER BY ms."Rolling30DayQuantity" DESC, ms."Rolling7DayQuantity" DESC, mi."Name" ASC
            LIMIT @TopItemsLimit;
            """;

        var topItems = (await conn.QueryAsync<TopItemRow>(
            new CommandDefinition(
                topItemsSql,
                new { request.RestaurantId, TopItemsLimit = topItemsLimit },
                cancellationToken: cancellationToken)))
            .ToList();

        var reviewDto = review is null
            ? new RestaurantDashboardReviewsDto(0, 0, null)
            : new RestaurantDashboardReviewsDto(review.AverageRating, review.TotalReviews, review.LastReviewAtUtc);

        var balanceDto = balance is null
            ? new RestaurantDashboardBalanceDto(0, "USD")
            : new RestaurantDashboardBalanceDto(balance.CurrentBalance, balance.Currency);

        var topItemDtos = topItems
            .Select(item => new RestaurantDashboardTopItemDto(
                item.MenuItemId,
                item.Name,
                item.Rolling7DayQuantity,
                item.Rolling30DayQuantity))
            .ToList();

        var topItemsUpdatedAtUtc = topItems.Count > 0
            ? topItems.Max(x => x.LastUpdatedAt)
            : (DateTime?)null;

        var lastUpdatedAtUtc = ResolveLastUpdatedAtUtc(
            summary.LastOrderAtUtc,
            review?.UpdatedAtUtc,
            topItemsUpdatedAtUtc);

        var dto = new RestaurantDashboardSummaryDto(
            new RestaurantDashboardRestaurantDto(
                summary.RestaurantId,
                summary.RestaurantName,
                summary.LogoUrl,
                summary.IsVerified,
                summary.IsAcceptingOrders),
            new RestaurantDashboardOrdersDto(
                summary.NewOrders,
                summary.ActiveOrders,
                summary.LastOrderAtUtc),
            new RestaurantDashboardSalesDto(
                summary.OrdersLast7Days,
                summary.OrdersLast30Days,
                summary.RevenueLast7Days,
                summary.RevenueLast30Days),
            reviewDto,
            topItemDtos,
            balanceDto,
            lastUpdatedAtUtc);

        return Result.Success(dto);
    }

    private static DateTime ResolveLastUpdatedAtUtc(DateTime? lastOrderAtUtc, DateTime? reviewUpdatedAtUtc, DateTime? topItemsUpdatedAtUtc)
    {
        var latest = DateTime.MinValue;

        if (lastOrderAtUtc.HasValue && lastOrderAtUtc.Value > latest)
        {
            latest = lastOrderAtUtc.Value;
        }

        if (reviewUpdatedAtUtc.HasValue && reviewUpdatedAtUtc.Value > latest)
        {
            latest = reviewUpdatedAtUtc.Value;
        }

        if (topItemsUpdatedAtUtc.HasValue && topItemsUpdatedAtUtc.Value > latest)
        {
            latest = topItemsUpdatedAtUtc.Value;
        }

        return latest == DateTime.MinValue ? DateTime.UtcNow : DateTime.SpecifyKind(latest, DateTimeKind.Utc);
    }

    private sealed record DashboardSummaryRow(
        Guid RestaurantId,
        string RestaurantName,
        string? LogoUrl,
        bool IsVerified,
        bool IsAcceptingOrders,
        int NewOrders,
        int ActiveOrders,
        DateTime? LastOrderAtUtc,
        int OrdersLast7Days,
        int OrdersLast30Days,
        decimal RevenueLast7Days,
        decimal RevenueLast30Days);

    private sealed record ReviewSummaryRow(
        double AverageRating,
        int TotalReviews,
        DateTime? LastReviewAtUtc,
        DateTime UpdatedAtUtc);

    private sealed record BalanceRow(
        decimal CurrentBalance,
        string Currency);

    private sealed record TopItemRow(
        Guid MenuItemId,
        string Name,
        long Rolling7DayQuantity,
        long Rolling30DayQuantity,
        DateTime LastUpdatedAt);
}

public sealed record RestaurantDashboardSummaryDto(
    RestaurantDashboardRestaurantDto Restaurant,
    RestaurantDashboardOrdersDto Orders,
    RestaurantDashboardSalesDto Sales,
    RestaurantDashboardReviewsDto Reviews,
    IReadOnlyList<RestaurantDashboardTopItemDto> TopItems,
    RestaurantDashboardBalanceDto Balance,
    DateTime UpdatedAtUtc);

public sealed record RestaurantDashboardRestaurantDto(
    Guid Id,
    string Name,
    string? LogoUrl,
    bool IsVerified,
    bool IsAcceptingOrders);

public sealed record RestaurantDashboardOrdersDto(
    int NewCount,
    int ActiveCount,
    DateTime? LastOrderAtUtc);

public sealed record RestaurantDashboardSalesDto(
    int OrdersLast7Days,
    int OrdersLast30Days,
    decimal RevenueLast7Days,
    decimal RevenueLast30Days);

public sealed record RestaurantDashboardReviewsDto(
    double AverageRating,
    int TotalReviews,
    DateTime? LastReviewAtUtc);

public sealed record RestaurantDashboardTopItemDto(
    Guid MenuItemId,
    string Name,
    long Rolling7DayQuantity,
    long Rolling30DayQuantity);

public sealed record RestaurantDashboardBalanceDto(
    decimal CurrentBalance,
    string Currency);

public static class RestaurantDashboardErrors
{
    public static Error NotFound => Error.NotFound(
        "Management.RestaurantDashboard.NotFound",
        "Restaurant was not found.");
}
