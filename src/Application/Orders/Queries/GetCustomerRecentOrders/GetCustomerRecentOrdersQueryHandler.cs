using System.Security.Claims;
using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Orders.Queries.GetCustomerRecentOrders;

/// <summary>
/// Handler for retrieving a paginated list of the authenticated customer's recent orders.
/// Uses Dapper + explicit SQL with <see cref="DapperPagination"/> helper to mirror EF pagination ergonomics.
/// Orders are sorted by placement timestamp (descending) then Id for deterministic ordering.
/// </summary>
public sealed class GetCustomerRecentOrdersQueryHandler : IRequestHandler<GetCustomerRecentOrdersQuery, Result<PaginatedList<OrderSummaryDto>>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IUser _currentUser;
    private readonly ILogger<GetCustomerRecentOrdersQueryHandler> _logger;

    public GetCustomerRecentOrdersQueryHandler(
        IDbConnectionFactory dbConnectionFactory,
        IUser currentUser,
        ILogger<GetCustomerRecentOrdersQueryHandler> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<PaginatedList<OrderSummaryDto>>> Handle(GetCustomerRecentOrdersQuery request, CancellationToken cancellationToken)
    {
        // Resolve current user id via common claim patterns
        var principal = _currentUser.Principal;
        var userIdClaim = principal?.FindFirst("sub")?.Value
                         ?? principal?.FindFirst("uid")?.Value
                         ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value; // fallback used in test infra

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            // Pipeline should normally block unauthorized access earlier, but guard defensively.
            _logger.LogWarning("Attempt to query customer recent orders without authenticated user context");
            throw new UnauthorizedAccessException();
        }

        using var connection = _dbConnectionFactory.CreateConnection();

        // Build page SQL using helper for consistency & safety
        const string selectColumns = """
            o."Id"                  AS OrderId,
            o."OrderNumber"         AS OrderNumber,
            o."Status"              AS Status,
            o."PlacementTimestamp"  AS PlacementTimestamp,
            o."RestaurantId"        AS RestaurantId,
            o."CustomerId"          AS CustomerId,
            o."TotalAmount_Amount"   AS TotalAmount,
            o."TotalAmount_Currency" AS TotalCurrency,
            CAST((SELECT COUNT(1)
                FROM "OrderItems" oi
            WHERE oi."OrderId" = o."Id") AS int) AS ItemCount
            """;

        const string fromAndWhere = """
            FROM "Orders" o
            WHERE o."CustomerId" = @CustomerId
            """;

        const string orderByClause = """
            o."PlacementTimestamp" DESC,
            o."Id" DESC
            """;

        var (countSql, pageSql) = DapperPagination.BuildPagedSql(
            selectColumns,
            fromAndWhere,
            orderByClause,
            request.PageNumber,
            request.PageSize);

        var parameters = new { CustomerId = userId };

        var page = await connection.QueryPageAsync<OrderSummaryDto>(
            countSql,
            pageSql,
            parameters,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        _logger.LogInformation(
            "Retrieved {Returned} of {Total} recent orders for customer {CustomerId} (page {Page}/{Size})",
            page.Items.Count,
            page.TotalCount,
            userId,
            request.PageNumber,
            request.PageSize);

        return Result.Success(page);
    }
}
