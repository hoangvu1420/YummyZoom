using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Orders.Queries.GetRestaurantActiveOrders;

/// <summary>
/// Handler for retrieving a paginated list of active orders for a restaurant.
/// Active statuses defined in <see cref="OrderQueryConstants.ActiveStatuses"/>. Orders are
/// ordered by status priority (Placed -> ReadyForDelivery), then placement timestamp ascending,
/// then Id to guarantee deterministic FIFO ordering.
/// </summary>
public sealed class GetRestaurantActiveOrdersQueryHandler : IRequestHandler<GetRestaurantActiveOrdersQuery, Result<PaginatedList<OrderSummaryDto>>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<GetRestaurantActiveOrdersQueryHandler> _logger;

    public GetRestaurantActiveOrdersQueryHandler(
        IDbConnectionFactory dbConnectionFactory,
        ILogger<GetRestaurantActiveOrdersQueryHandler> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    public async Task<Result<PaginatedList<OrderSummaryDto>>> Handle(GetRestaurantActiveOrdersQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string selectColumns = """
            o."Id"                  AS OrderId,
            o."OrderNumber"         AS OrderNumber,
            o."Status"              AS Status,
            o."PlacementTimestamp"  AS PlacementTimestamp,
            o."RestaurantId"        AS RestaurantId,
            (SELECT r."Name" FROM "Restaurants" r WHERE r."Id" = o."RestaurantId") AS RestaurantName,
            (SELECT r."LogoUrl" FROM "Restaurants" r WHERE r."Id" = o."RestaurantId") AS RestaurantImageUrl,
            o."CustomerId"          AS CustomerId,
            o."TotalAmount_Amount"   AS TotalAmount,
            o."TotalAmount_Currency" AS TotalCurrency,
            CAST((SELECT COUNT(1) FROM "OrderItems" oi WHERE oi."OrderId" = o."Id") AS int) AS ItemCount,
            o."SourceTeamCartId"     AS SourceTeamCartId,
            (o."SourceTeamCartId" IS NOT NULL) AS IsFromTeamCart,
            (SELECT COALESCE(SUM(CASE
                WHEN pt."Status" = 'Succeeded'
                 AND pt."Type" = 'Payment'
                 AND pt."PaymentMethodType" <> 'CashOnDelivery'
                    THEN pt."Transaction_Amount"
                ELSE 0
            END), 0)
            FROM "PaymentTransactions" pt
            WHERE pt."OrderId" = o."Id" AND pt."Type" = 'Payment') AS PaidOnlineAmount,
            (SELECT COALESCE(SUM(CASE
                WHEN pt."Status" = 'Succeeded'
                 AND pt."Type" = 'Payment'
                 AND pt."PaymentMethodType" = 'CashOnDelivery'
                    THEN pt."Transaction_Amount"
                ELSE 0
            END), 0)
            FROM "PaymentTransactions" pt
            WHERE pt."OrderId" = o."Id" AND pt."Type" = 'Payment') AS CashOnDeliveryAmount
            """;

        const string fromAndWhere = """
            FROM "Orders" o
            WHERE o."RestaurantId" = @RestaurantId
              AND o."Status" IN ('Placed','Accepted','Preparing','ReadyForDelivery')
            """;

        // Build CASE expression for priority ordering
        var statusCase = OrderQueryConstants.BuildStatusOrderCase();

        var orderByClause = $"{statusCase} ASC, o.\"PlacementTimestamp\" ASC, o.\"Id\" ASC";

        var (countSql, pageSql) = DapperPagination.BuildPagedSql(
            selectColumns,
            fromAndWhere,
            orderByClause,
            request.PageNumber,
            request.PageSize);

        var parameters = new { RestaurantId = request.RestaurantGuid };

        var page = await connection.QueryPageAsync<OrderSummaryDto>(
            countSql,
            pageSql,
            parameters,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        _logger.LogInformation(
            "Retrieved {Returned} of {Total} active orders for restaurant {RestaurantId} (page {Page}/{Size})",
            page.Items.Count,
            page.TotalCount,
            request.RestaurantGuid,
            request.PageNumber,
            request.PageSize);

        return Result.Success(page);
    }
}
