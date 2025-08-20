using Dapper;
using System.Data;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.SharedKernel;
using Microsoft.Extensions.Logging;
using YummyZoom.SharedKernel.Constants;
using System.Security.Claims;

namespace YummyZoom.Application.Orders.Queries.GetOrderById;

/// <summary>
/// Handler that loads an order with its line items + customizations using two SQL queries
/// to keep logic simple and predictable. JSON customizations are parsed in-process.
/// </summary>
public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<GetOrderByIdResponse>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IUser _currentUser;
    private readonly ILogger<GetOrderByIdQueryHandler> _logger;

    public GetOrderByIdQueryHandler(
        IDbConnectionFactory dbConnectionFactory,
        IUser currentUser,
        ILogger<GetOrderByIdQueryHandler> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<GetOrderByIdResponse>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        // 1. Load order base row
        const string orderSql = """
            SELECT
                o."Id"               AS OrderId,
                o."OrderNumber"      AS OrderNumber,
                o."CustomerId"       AS CustomerId,
                o."RestaurantId"     AS RestaurantId,
                o."Status"           AS Status,
                o."PlacementTimestamp"      AS PlacementTimestamp,
                o."LastUpdateTimestamp"     AS LastUpdateTimestamp,
                o."EstimatedDeliveryTime"   AS EstimatedDeliveryTime,
                o."ActualDeliveryTime"      AS ActualDeliveryTime,
                o."Subtotal_Amount"         AS SubtotalAmount,
                o."Subtotal_Currency"       AS SubtotalCurrency,
                o."DiscountAmount_Amount"   AS DiscountAmount,
                o."DiscountAmount_Currency" AS DiscountCurrency,
                o."DeliveryFee_Amount"      AS DeliveryFeeAmount,
                o."DeliveryFee_Currency"    AS DeliveryFeeCurrency,
                o."TipAmount_Amount"        AS TipAmount,
                o."TipAmount_Currency"      AS TipCurrency,
                o."TaxAmount_Amount"        AS TaxAmount,
                o."TaxAmount_Currency"      AS TaxCurrency,
                o."TotalAmount_Amount"      AS TotalAmount,
                o."TotalAmount_Currency"    AS TotalCurrency,
                o."AppliedCouponId"         AS AppliedCouponId,
                o."SourceTeamCartId"        AS SourceTeamCartId,
                o."DeliveryAddress_Street"      AS DeliveryAddress_Street,
                o."DeliveryAddress_City"        AS DeliveryAddress_City,
                o."DeliveryAddress_State"       AS DeliveryAddress_State,
                o."DeliveryAddress_Country"     AS DeliveryAddress_Country,
                o."DeliveryAddress_ZipCode"     AS DeliveryAddress_PostalCode
            FROM "Orders" o
            WHERE o."Id" = @OrderId
            """;

        var orderRow = await connection.QuerySingleOrDefaultAsync<OrderDetailsRow>(
            new CommandDefinition(orderSql, new { OrderId = request.OrderIdGuid }, cancellationToken: cancellationToken));

        if (orderRow == null)
        {
            _logger.LogInformation("Order {OrderId} not found", request.OrderIdGuid);
            return Result.Failure<GetOrderByIdResponse>(GetOrderByIdErrors.NotFound);
        }

        // 2. Authorization check (customer or restaurant staff). If user not authenticated, pipeline would already block.
        var principal = _currentUser.Principal;
        // Support multiple possible claim types for user id: OIDC (sub), custom (uid), and NameIdentifier (used in test infrastructure)
        var userIdClaim = principal?.FindFirst("sub")?.Value
                  ?? principal?.FindFirst("uid")?.Value
                  ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value; // fallback for tests / generic
        var isCustomer = userIdClaim != null && Guid.TryParse(userIdClaim, out var userGuid) && userGuid == orderRow.CustomerId;
        
        // Check for restaurant staff/owner permission using permission claims
        var restaurantIdString = orderRow.RestaurantId.ToString();
        var isRestaurantStaff = principal?.HasClaim("permission", $"{Roles.RestaurantStaff}:{restaurantIdString}") == true
                                || principal?.HasClaim("permission", $"{Roles.RestaurantOwner}:{restaurantIdString}") == true
                                || principal?.IsInRole(Roles.Administrator) == true;

        if (!isCustomer && !isRestaurantStaff)
        {
            _logger.LogWarning("Unauthorized access attempt to order {OrderId} by user {User}", request.OrderIdGuid, userIdClaim);
            // Return NotFound to avoid leaking existence
            return Result.Failure<GetOrderByIdResponse>(GetOrderByIdErrors.NotFound);
        }

        // 3. Load items
        const string itemsSql = """
            SELECT
                i."OrderItemId"              AS OrderItemId,
                i."Snapshot_MenuItemId"      AS MenuItemId,
                i."Snapshot_ItemName"        AS Name,
                i."Quantity"                 AS Quantity,
                i."BasePrice_Amount"         AS UnitPriceAmount,
                i."BasePrice_Currency"       AS UnitPriceCurrency,
                i."LineItemTotal_Amount"     AS LineItemTotalAmount,
                i."LineItemTotal_Currency"   AS LineItemTotalCurrency,
                i."SelectedCustomizations"   AS SelectedCustomizations
            FROM "OrderItems" i
            WHERE i."OrderId" = @OrderId
            ORDER BY i."OrderItemId"
            """;

        var itemRows = await connection.QueryAsync<OrderItemRow>(
            new CommandDefinition(itemsSql, new { OrderId = request.OrderIdGuid }, cancellationToken: cancellationToken));

        var itemDtos = itemRows
            .Select(r => new OrderItemDto(
                r.OrderItemId,
                r.MenuItemId,
                r.Name,
                r.Quantity,
                r.UnitPriceAmount,
                r.UnitPriceCurrency,
                r.LineItemTotalAmount,
                r.LineItemTotalCurrency,
                OrderCustomizationJsonParser.Parse(r.SelectedCustomizations)))
            .ToList();

        var details = new OrderDetailsDto(
            orderRow.OrderId,
            orderRow.OrderNumber,
            orderRow.CustomerId,
            orderRow.RestaurantId,
            orderRow.Status,
            orderRow.PlacementTimestamp,
            orderRow.LastUpdateTimestamp,
            orderRow.EstimatedDeliveryTime,
            orderRow.ActualDeliveryTime,
            orderRow.SubtotalAmount,
            orderRow.SubtotalCurrency,
            orderRow.DiscountAmount,
            orderRow.DiscountCurrency,
            orderRow.DeliveryFeeAmount,
            orderRow.DeliveryFeeCurrency,
            orderRow.TipAmount,
            orderRow.TipCurrency,
            orderRow.TaxAmount,
            orderRow.TaxCurrency,
            orderRow.TotalAmount,
            orderRow.TotalCurrency,
            orderRow.AppliedCouponId,
            orderRow.SourceTeamCartId,
            orderRow.DeliveryAddress_Street,
            orderRow.DeliveryAddress_City,
            orderRow.DeliveryAddress_State,
            orderRow.DeliveryAddress_Country,
            orderRow.DeliveryAddress_PostalCode,
            itemDtos);

        _logger.LogInformation("Order {OrderId} retrieved with {ItemCount} items", request.OrderIdGuid, itemDtos.Count);
        return Result.Success(new GetOrderByIdResponse(details));
    }

    // Internal row-shaping types for Dapper materialization (avoid leaking into public API)
    private sealed record OrderDetailsRow(
        Guid OrderId,
        string OrderNumber,
        Guid CustomerId,
        Guid RestaurantId,
        string Status,
        DateTime PlacementTimestamp,
        DateTime LastUpdateTimestamp,
        DateTime? EstimatedDeliveryTime,
        DateTime? ActualDeliveryTime,
        decimal SubtotalAmount,
        string SubtotalCurrency,
        decimal DiscountAmount,
        string DiscountCurrency,
        decimal DeliveryFeeAmount,
        string DeliveryFeeCurrency,
        decimal TipAmount,
        string TipCurrency,
        decimal TaxAmount,
        string TaxCurrency,
        decimal TotalAmount,
        string TotalCurrency,
        Guid? AppliedCouponId,
        Guid? SourceTeamCartId,
        string? DeliveryAddress_Street,
        string? DeliveryAddress_City,
        string? DeliveryAddress_State,
        string? DeliveryAddress_Country,
        string? DeliveryAddress_PostalCode);

    private sealed record OrderItemRow(
        Guid OrderItemId,
        Guid MenuItemId,
        string Name,
        int Quantity,
        decimal UnitPriceAmount,
        string UnitPriceCurrency,
        decimal LineItemTotalAmount,
        string LineItemTotalCurrency,
        string? SelectedCustomizations);
}
