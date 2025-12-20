using System.Text;
using Dapper;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Orders.Queries.GetRestaurantOrderHistory;

/// <summary>
/// Handler for retrieving a paginated list of historical orders for a restaurant.
/// Orders are sorted by placement timestamp descending for recent-first history views.
/// </summary>
public sealed class GetRestaurantOrderHistoryQueryHandler
    : IRequestHandler<GetRestaurantOrderHistoryQuery, Result<PaginatedList<OrderHistorySummaryDto>>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<GetRestaurantOrderHistoryQueryHandler> _logger;

    public GetRestaurantOrderHistoryQueryHandler(
        IDbConnectionFactory dbConnectionFactory,
        ILogger<GetRestaurantOrderHistoryQueryHandler> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    public async Task<Result<PaginatedList<OrderHistorySummaryDto>>> Handle(GetRestaurantOrderHistoryQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string selectColumns = """
            o."Id"                  AS OrderId,
            o."OrderNumber"         AS OrderNumber,
            o."Status"              AS Status,
            o."PlacementTimestamp"  AS PlacementTimestamp,
            CASE
                WHEN o."Status" = 'Delivered' THEN COALESCE(o."ActualDeliveryTime", o."LastUpdateTimestamp")
                WHEN o."Status" IN ('Cancelled','Rejected') THEN o."LastUpdateTimestamp"
                ELSE NULL
            END AS CompletedTimestamp,
            o."TotalAmount_Amount"   AS TotalAmount,
            o."TotalAmount_Currency" AS TotalCurrency,
            CAST((SELECT COUNT(1) FROM "OrderItems" oi WHERE oi."OrderId" = o."Id") AS int) AS ItemCount,
            u."Name"                AS CustomerName,
            u."PhoneNumber"         AS CustomerPhone,
            (SELECT CASE pt."Status"
                WHEN 'Succeeded' THEN 'Paid'
                WHEN 'Failed' THEN 'Failed'
                WHEN 'Pending' THEN 'Pending'
                ELSE pt."Status"
             END
             FROM "PaymentTransactions" pt
             WHERE pt."OrderId" = o."Id" AND pt."Type" = 'Payment'
             ORDER BY pt."Timestamp" DESC
             LIMIT 1) AS PaymentStatus,
            (SELECT pt."PaymentMethodType"
             FROM "PaymentTransactions" pt
             WHERE pt."OrderId" = o."Id" AND pt."Type" = 'Payment'
             ORDER BY pt."Timestamp" ASC
             LIMIT 1) AS PaymentMethod
            """;

        var fromAndWhere = new StringBuilder("""
            FROM "Orders" o
            LEFT JOIN "DomainUsers" u ON u."Id" = o."CustomerId"
            WHERE o."RestaurantId" = @RestaurantId
              AND o."Status" = ANY(@HistoryStatuses)
            """);

        var parameters = new DynamicParameters();
        parameters.Add("RestaurantId", request.RestaurantGuid);
        parameters.Add("HistoryStatuses", OrderQueryConstants.HistoryStatuses);

        if (request.From.HasValue)
        {
            fromAndWhere.Append(@" AND o.""PlacementTimestamp"" >= @From");
            parameters.Add("From", request.From.Value);
        }

        if (request.To.HasValue)
        {
            fromAndWhere.Append(@" AND o.""PlacementTimestamp"" <= @To");
            parameters.Add("To", request.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            fromAndWhere.Append(@" AND (o.""OrderNumber"" ILIKE @Keyword OR u.""Name"" ILIKE @Keyword OR u.""PhoneNumber"" ILIKE @Keyword)");
            parameters.Add("Keyword", $"%{request.Keyword}%");
        }

        if (!string.IsNullOrWhiteSpace(request.Statuses))
        {
            var statuses = request.Statuses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (statuses.Length > 0)
            {
                fromAndWhere.Append(@" AND o.""Status"" = ANY(@Statuses)");
                parameters.Add("Statuses", statuses);
            }
        }

        const string orderByClause = """
            o."PlacementTimestamp" DESC,
            o."Id" DESC
            """;

        var (countSql, pageSql) = DapperPagination.BuildPagedSql(
            selectColumns,
            fromAndWhere.ToString(),
            orderByClause,
            request.PageNumber,
            request.PageSize);

        var page = await connection.QueryPageAsync<OrderHistorySummaryDto>(
            countSql,
            pageSql,
            parameters,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        _logger.LogInformation(
            "Retrieved {Returned} of {Total} historical orders for restaurant {RestaurantId} (page {Page}/{Size})",
            page.Items.Count,
            page.TotalCount,
            request.RestaurantGuid,
            request.PageNumber,
            request.PageSize);

        return Result.Success(page);
    }
}
