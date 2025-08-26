using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Orders.Queries.GetRestaurantNewOrders;

/// <summary>
/// Handler for retrieving paginated list of newly placed (Status = Placed) orders for a restaurant.
/// Orders sorted oldest-first (PlacementTimestamp ASC) to support FIFO processing in operational dashboards.
/// </summary>
public sealed class GetRestaurantNewOrdersQueryHandler : IRequestHandler<GetRestaurantNewOrdersQuery, Result<PaginatedList<OrderSummaryDto>>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<GetRestaurantNewOrdersQueryHandler> _logger;

    public GetRestaurantNewOrdersQueryHandler(
        IDbConnectionFactory dbConnectionFactory,
        ILogger<GetRestaurantNewOrdersQueryHandler> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    public async Task<Result<PaginatedList<OrderSummaryDto>>> Handle(GetRestaurantNewOrdersQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string selectColumns = """
            o."Id"                  AS OrderId,
            o."OrderNumber"         AS OrderNumber,
            o."Status"              AS Status,
            o."PlacementTimestamp"  AS PlacementTimestamp,
            o."RestaurantId"        AS RestaurantId,
            o."CustomerId"          AS CustomerId,
            o."TotalAmount_Amount"   AS TotalAmount,
            o."TotalAmount_Currency" AS TotalCurrency,
            CAST((SELECT COUNT(1) FROM "OrderItems" oi WHERE oi."OrderId" = o."Id") AS int) AS ItemCount
            """;

        const string fromAndWhere = """
            FROM "Orders" o
            WHERE o."RestaurantId" = @RestaurantId
              AND o."Status" = 'Placed'
            """;

        const string orderByClause = """
            o."PlacementTimestamp" ASC,
            o."Id" ASC
            """;

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
            "Retrieved {Returned} of {Total} new orders for restaurant {RestaurantId} (page {Page}/{Size})",
            page.Items.Count,
            page.TotalCount,
            request.RestaurantGuid,
            request.PageNumber,
            request.PageSize);

        return Result.Success(page);
    }
}
