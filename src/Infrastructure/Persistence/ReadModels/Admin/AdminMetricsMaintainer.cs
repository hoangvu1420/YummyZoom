using System;
using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Caching;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IServices;

namespace YummyZoom.Infrastructure.Persistence.ReadModels.Admin;

public sealed class AdminMetricsMaintainer : IAdminMetricsMaintainer
{
    private readonly IDbConnectionFactory _db;
    private readonly ICacheInvalidationPublisher _invalidationPublisher;
    private readonly ILogger<AdminMetricsMaintainer> _logger;

    private const string SnapshotId = "platform";

    public AdminMetricsMaintainer(
        IDbConnectionFactory db,
        ICacheInvalidationPublisher invalidationPublisher,
        ILogger<AdminMetricsMaintainer> logger)
    {
        _db = db;
        _invalidationPublisher = invalidationPublisher;
        _logger = logger;
    }

    public async Task RecomputeAllAsync(int dailySeriesWindowDays, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var transaction = conn.BeginTransaction();

        try
        {
            await RecomputePlatformSnapshotAsync(conn, transaction, ct);
            await RecomputeDailySeriesAsync(conn, transaction, dailySeriesWindowDays, ct);
            await RecomputeRestaurantHealthAsync(conn, transaction, ct);

            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "AdminMetricsMaintainer: recompute failed");
            throw;
        }

        await PublishInvalidationAsync(ct);
    }

    private static Task RecomputePlatformSnapshotAsync(IDbConnection conn, IDbTransaction transaction, CancellationToken ct)
    {
        const string sql = """
WITH order_stats AS (
    SELECT
        COUNT(*)::bigint                                         AS total_orders,
        COUNT(*) FILTER (WHERE o."Status" IN ('Placed','Accepted','Preparing','ReadyForDelivery'))::bigint AS active_orders,
        COUNT(*) FILTER (WHERE o."Status" = 'Delivered')::bigint AS delivered_orders,
        COALESCE(SUM(CASE WHEN o."Status" = 'Delivered' THEN o."TotalAmount_Amount" ELSE 0 END), 0)::decimal(18,2) AS gmv,
        MAX(o."PlacementTimestamp")                              AS last_order_at
    FROM "Orders" o
),
refund_stats AS (
    SELECT COALESCE(SUM(-a."Amount"), 0)::decimal(18,2) AS refunds
    FROM "AccountTransactions" a
    WHERE a."Type" = 'RefundDeduction'
),
active_restaurants AS (
    SELECT COUNT(*)::int AS count
    FROM "Restaurants" r
    WHERE r."IsDeleted" = FALSE AND r."IsVerified" = TRUE
),
active_customers AS (
    SELECT COUNT(*)::int AS count
    FROM "DomainUsers" u
    WHERE u."IsDeleted" = FALSE AND u."IsActive" = TRUE
),
open_support_tickets AS (
    SELECT COUNT(*)::int AS count
    FROM "SupportTickets" s
    WHERE s."Status" IN ('Open','InProgress','PendingCustomerResponse')
),
review_stats AS (
    SELECT COALESCE(SUM(s."TotalReviews"), 0)::int AS total_reviews
    FROM "RestaurantReviewSummaries" s
)
INSERT INTO "AdminPlatformMetricsSnapshots" (
    "SnapshotId",
    "TotalOrders",
    "ActiveOrders",
    "DeliveredOrders",
    "GrossMerchandiseVolume",
    "TotalRefunds",
    "ActiveRestaurants",
    "ActiveCustomers",
    "OpenSupportTickets",
    "TotalReviews",
    "LastOrderAtUtc",
    "UpdatedAtUtc")
SELECT
    @SnapshotId,
    order_stats.total_orders,
    order_stats.active_orders,
    order_stats.delivered_orders,
    order_stats.gmv,
    refund_stats.refunds,
    active_restaurants.count,
    active_customers.count,
    open_support_tickets.count,
    review_stats.total_reviews,
    order_stats.last_order_at,
    NOW()
FROM order_stats, refund_stats, active_restaurants, active_customers, open_support_tickets, review_stats
ON CONFLICT ("SnapshotId") DO UPDATE SET
    "TotalOrders" = EXCLUDED."TotalOrders",
    "ActiveOrders" = EXCLUDED."ActiveOrders",
    "DeliveredOrders" = EXCLUDED."DeliveredOrders",
    "GrossMerchandiseVolume" = EXCLUDED."GrossMerchandiseVolume",
    "TotalRefunds" = EXCLUDED."TotalRefunds",
    "ActiveRestaurants" = EXCLUDED."ActiveRestaurants",
    "ActiveCustomers" = EXCLUDED."ActiveCustomers",
    "OpenSupportTickets" = EXCLUDED."OpenSupportTickets",
    "TotalReviews" = EXCLUDED."TotalReviews",
    "LastOrderAtUtc" = EXCLUDED."LastOrderAtUtc",
    "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc";
""";

        var command = new CommandDefinition(
            sql,
            new { SnapshotId },
            transaction: transaction,
            cancellationToken: ct);

        return conn.ExecuteAsync(command);
    }

    private static async Task RecomputeDailySeriesAsync(IDbConnection conn, IDbTransaction transaction, int windowDays, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
                var start = today.AddDays(-Math.Max(windowDays - 1, 0));
        var startDate = start.ToDateTime(TimeOnly.MinValue);
        var endDate = today.ToDateTime(TimeOnly.MinValue);

        const string deleteSql = "DELETE FROM \"AdminDailyPerformanceSeries\" WHERE \"BucketDate\" BETWEEN @Start AND @End;";
        await conn.ExecuteAsync(new CommandDefinition(deleteSql, new { Start = startDate, End = endDate }, transaction, cancellationToken: ct));

        const string insertSql = """
WITH date_range AS (
    SELECT generate_series(@Start::date, @End::date, '1 day')::date AS bucket_date
),
order_daily AS (
    SELECT
        DATE(o."PlacementTimestamp")                             AS bucket_date,
        COUNT(*)::int                                             AS total_orders,
        COUNT(*) FILTER (WHERE o."Status" = 'Delivered')::int     AS delivered_orders,
        COALESCE(SUM(CASE WHEN o."Status" = 'Delivered' THEN o."TotalAmount_Amount" ELSE 0 END), 0)::decimal(18,2) AS gmv
    FROM "Orders" o
    WHERE o."PlacementTimestamp" >= @Start::timestamp
    GROUP BY 1
),
refund_daily AS (
    SELECT
        DATE(a."Timestamp")                                     AS bucket_date,
        COALESCE(SUM(-a."Amount"), 0)::decimal(18,2)            AS refunds
    FROM "AccountTransactions" a
    WHERE a."Type" = 'RefundDeduction' AND a."Timestamp" >= @Start::timestamp
    GROUP BY 1
),
new_customers AS (
    SELECT DATE(u."Created") AS bucket_date, COUNT(*)::int AS new_customers
    FROM "DomainUsers" u
    WHERE u."Created" >= @Start::timestamp
    GROUP BY 1
),
new_restaurants AS (
    SELECT DATE(r."Created") AS bucket_date, COUNT(*)::int AS new_restaurants
    FROM "Restaurants" r
    WHERE r."Created" >= @Start::timestamp AND r."IsDeleted" = FALSE
    GROUP BY 1
)
INSERT INTO "AdminDailyPerformanceSeries" (
    "BucketDate",
    "TotalOrders",
    "DeliveredOrders",
    "GrossMerchandiseVolume",
    "TotalRefunds",
    "NewCustomers",
    "NewRestaurants",
    "UpdatedAtUtc")
SELECT
    dr.bucket_date,
    COALESCE(od.total_orders, 0),
    COALESCE(od.delivered_orders, 0),
    COALESCE(od.gmv, 0),
    COALESCE(rd.refunds, 0),
    COALESCE(nc.new_customers, 0),
    COALESCE(nr.new_restaurants, 0),
    NOW()
FROM date_range dr
LEFT JOIN order_daily od ON od.bucket_date = dr.bucket_date
LEFT JOIN refund_daily rd ON rd.bucket_date = dr.bucket_date
LEFT JOIN new_customers nc ON nc.bucket_date = dr.bucket_date
LEFT JOIN new_restaurants nr ON nr.bucket_date = dr.bucket_date;
""";

        var insertCommand = new CommandDefinition(
            insertSql,
            new { Start = startDate, End = endDate },
            transaction,
            cancellationToken: ct);

        await conn.ExecuteAsync(insertCommand);
    }

    private static async Task RecomputeRestaurantHealthAsync(IDbConnection conn, IDbTransaction transaction, CancellationToken ct)
    {
        const string deleteSql = "DELETE FROM \"AdminRestaurantHealthSummaries\";";
        await conn.ExecuteAsync(new CommandDefinition(deleteSql, transaction: transaction, cancellationToken: ct));

        const string insertSql = """
WITH recent_orders AS (
    SELECT
        o."RestaurantId"                                    AS restaurant_id,
        COUNT(*) FILTER (WHERE o."PlacementTimestamp" >= NOW() - INTERVAL '7 days')::int AS orders_7d,
        COUNT(*) FILTER (WHERE o."PlacementTimestamp" >= NOW() - INTERVAL '30 days')::int AS orders_30d,
        COALESCE(SUM(CASE WHEN o."Status" = 'Delivered' AND o."PlacementTimestamp" >= NOW() - INTERVAL '30 days'
                          THEN o."TotalAmount_Amount" ELSE 0 END), 0)::decimal(18,2) AS revenue_30d,
        MAX(o."PlacementTimestamp")                         AS last_order_at
    FROM "Orders" o
    GROUP BY o."RestaurantId"
),
coupon_usage AS (
    SELECT
        o."RestaurantId"                                    AS restaurant_id,
        COUNT(*) FILTER (WHERE o."AppliedCouponId" IS NOT NULL AND o."PlacementTimestamp" >= NOW() - INTERVAL '30 days')::int AS coupon_redemptions_30d
    FROM "Orders" o
    GROUP BY o."RestaurantId"
)
INSERT INTO "AdminRestaurantHealthSummaries" (
    "RestaurantId",
    "RestaurantName",
    "IsVerified",
    "IsAcceptingOrders",
    "OrdersLast7Days",
    "OrdersLast30Days",
    "RevenueLast30Days",
    "AverageRating",
    "TotalReviews",
    "CouponRedemptionsLast30Days",
    "OutstandingBalance",
    "LastOrderAtUtc",
    "UpdatedAtUtc")
SELECT
    r."Id",
    r."Name",
    r."IsVerified",
    r."IsAcceptingOrders",
    COALESCE(ro.orders_7d, 0),
    COALESCE(ro.orders_30d, 0),
    COALESCE(ro.revenue_30d, 0),
    COALESCE(rrs."AverageRating", 0),
    COALESCE(rrs."TotalReviews", 0),
    COALESCE(cu.coupon_redemptions_30d, 0),
    COALESCE(ra."CurrentBalance_Amount", 0),
    ro.last_order_at,
    NOW()
FROM "Restaurants" r
LEFT JOIN recent_orders ro ON ro.restaurant_id = r."Id"
LEFT JOIN coupon_usage cu ON cu.restaurant_id = r."Id"
LEFT JOIN "RestaurantReviewSummaries" rrs ON rrs."RestaurantId" = r."Id"
LEFT JOIN "RestaurantAccounts" ra ON ra."RestaurantId" = r."Id"
WHERE r."IsDeleted" = FALSE;
""";

        await conn.ExecuteAsync(new CommandDefinition(insertSql, transaction: transaction, cancellationToken: ct));
    }

    private async Task PublishInvalidationAsync(CancellationToken ct)
    {
        var message = new CacheInvalidationMessage
        {
            Tags = new[] { "cache:admin:platform-metrics" },
            Reason = "AdminMetricsMaintenance",
            SourceEvent = nameof(AdminMetricsMaintainer)
        };

        try
        {
            await _invalidationPublisher.PublishAsync(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AdminMetricsMaintainer: cache invalidation publish failed");
        }
    }
}
