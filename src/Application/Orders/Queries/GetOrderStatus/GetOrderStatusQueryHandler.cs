using System.Security.Claims;
using Dapper;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Orders.Queries.GetOrderStatus;

/// <summary>
/// Handler that retrieves a lean status projection for a single order.
/// Designed for high-frequency polling (e.g., client refresh) without overfetching full order details.
/// Future enhancement: add ETag generation (hash of Status + LastUpdateTimestamp) for conditional responses.
/// </summary>
public sealed class GetOrderStatusQueryHandler : IRequestHandler<GetOrderStatusQuery, Result<OrderStatusDto>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IUser _currentUser;
    private readonly ILogger<GetOrderStatusQueryHandler> _logger;

    public GetOrderStatusQueryHandler(
        IDbConnectionFactory dbConnectionFactory,
        IUser currentUser,
        ILogger<GetOrderStatusQueryHandler> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<OrderStatusDto>> Handle(GetOrderStatusQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                o."Id"                      AS OrderId,
                o."Status"                  AS Status,
                o."LastUpdateTimestamp"     AS LastUpdateTimestamp,
                o."EstimatedDeliveryTime"   AS EstimatedDeliveryTime,
                o."CustomerId"              AS CustomerId,
                o."RestaurantId"            AS RestaurantId
            FROM "Orders" o
            WHERE o."Id" = @OrderId
            """;

        var row = await connection.QuerySingleOrDefaultAsync<OrderStatusRow>(
            new CommandDefinition(sql, new { OrderId = request.OrderIdGuid }, cancellationToken: cancellationToken));

        if (row is null)
        {
            _logger.LogInformation("Order {OrderId} not found (status query)", request.OrderIdGuid);
            return Result.Failure<OrderStatusDto>(GetOrderStatusErrors.NotFound);
        }

        // Authorization: customer or restaurant staff / owner / admin
        var principal = _currentUser.Principal;
        var userIdClaim = principal?.FindFirst("sub")?.Value
                           ?? principal?.FindFirst("uid")?.Value
                           ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = userIdClaim != null && Guid.TryParse(userIdClaim, out var userGuid) && userGuid == row.CustomerId;
        var restaurantIdString = row.RestaurantId.ToString();
        var isRestaurantStaff = principal?.HasClaim("permission", $"{Roles.RestaurantStaff}:{restaurantIdString}") == true
                                || principal?.HasClaim("permission", $"{Roles.RestaurantOwner}:{restaurantIdString}") == true
                                || principal?.IsInRole(Roles.Administrator) == true;

        if (!isCustomer && !isRestaurantStaff)
        {
            _logger.LogWarning("Unauthorized status access attempt for order {OrderId} by user {User}", request.OrderIdGuid, userIdClaim);
            return Result.Failure<OrderStatusDto>(GetOrderStatusErrors.NotFound);
        }

        var dto = new OrderStatusDto(row.OrderId, row.Status, row.LastUpdateTimestamp, row.EstimatedDeliveryTime);
        _logger.LogInformation("Order status retrieved {OrderId} = {Status}", row.OrderId, row.Status);
        return Result.Success(dto);
    }

    private sealed record OrderStatusRow(
        Guid OrderId,
        string Status,
        DateTime LastUpdateTimestamp,
        DateTime? EstimatedDeliveryTime,
        Guid CustomerId,
        Guid RestaurantId);
}
