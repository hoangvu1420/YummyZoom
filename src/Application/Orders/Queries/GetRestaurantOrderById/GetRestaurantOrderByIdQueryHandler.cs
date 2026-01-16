using Dapper;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Orders.Queries.GetRestaurantOrderById;

/// <summary>
/// Loads order details for a restaurant-scoped staff view.
/// Uses restaurantId + orderId in SQL to avoid leaking order existence across tenants.
/// </summary>
public sealed class GetRestaurantOrderByIdQueryHandler : IRequestHandler<GetRestaurantOrderByIdQuery, Result<OrderDetailsDto>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<GetRestaurantOrderByIdQueryHandler> _logger;

    public GetRestaurantOrderByIdQueryHandler(
        IDbConnectionFactory dbConnectionFactory,
        ILogger<GetRestaurantOrderByIdQueryHandler> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    public async Task<Result<OrderDetailsDto>> Handle(GetRestaurantOrderByIdQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        // Use QueryMultiple to reduce DB round-trips (single command: order + items + payment).
        const string sql = """
            SELECT
                o."Id"                      AS OrderId,
                o."OrderNumber"             AS OrderNumber,
                o."CustomerId"              AS CustomerId,
                o."RestaurantId"            AS RestaurantId,
                o."Status"                  AS Status,
                o."PlacementTimestamp"      AS PlacementTimestamp,
                o."LastUpdateTimestamp"     AS LastUpdateTimestamp,
                o."EstimatedDeliveryTime"   AS EstimatedDeliveryTime,
                o."ActualDeliveryTime"      AS ActualDeliveryTime,
                o."SpecialInstructions"     AS Note,
                o."Subtotal_Currency"       AS Currency,
                o."Subtotal_Amount"         AS SubtotalAmount,
                o."DiscountAmount_Amount"   AS DiscountAmount,
                o."DeliveryFee_Amount"      AS DeliveryFeeAmount,
                o."TipAmount_Amount"        AS TipAmount,
                o."TaxAmount_Amount"        AS TaxAmount,
                o."TotalAmount_Amount"      AS TotalAmount,
                o."AppliedCouponId"         AS AppliedCouponId,
                o."SourceTeamCartId"        AS SourceTeamCartId,
                o."DeliveryAddress_Street"  AS DeliveryAddress_Street,
                o."DeliveryAddress_City"    AS DeliveryAddress_City,
                o."DeliveryAddress_State"   AS DeliveryAddress_State,
                o."DeliveryAddress_Country" AS DeliveryAddress_Country,
                o."DeliveryAddress_ZipCode" AS DeliveryAddress_PostalCode,
                r."Name"                     AS RestaurantName,
                r."Location_Street"          AS RestaurantAddress_Street,
                r."Location_City"            AS RestaurantAddress_City,
                r."Location_State"           AS RestaurantAddress_State,
                r."Location_Country"         AS RestaurantAddress_Country,
                r."Location_ZipCode"         AS RestaurantAddress_PostalCode,
                r."Geo_Latitude"             AS RestaurantLat,
                r."Geo_Longitude"            AS RestaurantLon
            FROM "Orders" o
            LEFT JOIN "Restaurants" r ON r."Id" = o."RestaurantId"
            WHERE o."Id" = @OrderId AND o."RestaurantId" = @RestaurantId;

            SELECT
                i."OrderItemId"              AS OrderItemId,
                i."Snapshot_MenuItemId"      AS MenuItemId,
                i."Snapshot_ItemName"        AS Name,
                i."Quantity"                 AS Quantity,
                i."BasePrice_Amount"         AS UnitPriceAmount,
                i."LineItemTotal_Amount"     AS LineItemTotalAmount,
                i."SelectedCustomizations"   AS SelectedCustomizations,
                mi."ImageUrl"                AS ImageUrl
            FROM "OrderItems" i
            LEFT JOIN "MenuItems" mi ON mi."Id" = i."Snapshot_MenuItemId"
            WHERE i."OrderId" = @OrderId
            ORDER BY i."OrderItemId";

            SELECT pt."PaymentMethodType" AS PaymentMethod
            FROM "PaymentTransactions" pt
            WHERE pt."OrderId" = @OrderId AND pt."Type" = 'Payment'
            ORDER BY pt."Timestamp" ASC
            LIMIT 1;

            SELECT
                COALESCE(SUM(CASE
                    WHEN pt."Status" = 'Succeeded'
                     AND pt."Type" = 'Payment'
                     AND pt."PaymentMethodType" <> 'CashOnDelivery'
                        THEN pt."Transaction_Amount"
                    ELSE 0
                END), 0) AS PaidOnlineAmount,
                COALESCE(SUM(CASE
                    WHEN pt."Status" = 'Succeeded'
                     AND pt."Type" = 'Payment'
                     AND pt."PaymentMethodType" = 'CashOnDelivery'
                        THEN pt."Transaction_Amount"
                    ELSE 0
                END), 0) AS CashOnDeliveryAmount
            FROM "PaymentTransactions" pt
            WHERE pt."OrderId" = @OrderId
              AND pt."Type" = 'Payment'
              AND pt."Transaction_Currency" = (SELECT o."Subtotal_Currency" FROM "Orders" o WHERE o."Id" = @OrderId);
            """;

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(
                sql,
                new { OrderId = request.OrderIdGuid, RestaurantId = request.RestaurantGuid },
                cancellationToken: cancellationToken));

        var orderRow = await multi.ReadSingleOrDefaultAsync<OrderDetailsRow>();

        if (orderRow == null)
        {
            _logger.LogInformation(
                "Order {OrderId} not found for restaurant {RestaurantId}",
                request.OrderIdGuid,
                request.RestaurantGuid);
            return Result.Failure<OrderDetailsDto>(GetRestaurantOrderByIdErrors.NotFound);
        }

        var itemRows = await multi.ReadAsync<OrderItemRow>();

        var itemDtos = itemRows
            .Select(r => new OrderItemDto(
                r.OrderItemId,
                r.MenuItemId,
                r.Name,
                r.Quantity,
                r.UnitPriceAmount,
                r.LineItemTotalAmount,
                OrderCustomizationJsonParser.Parse(r.SelectedCustomizations),
                r.ImageUrl))
            .ToList();

        var paymentMethod = await multi.ReadSingleOrDefaultAsync<string?>();
        var paymentSplit = await multi.ReadSingleAsync<PaymentSplitRow>();

        bool cancellable = false;
        try
        {
            if (Enum.TryParse<YummyZoom.Domain.OrderAggregate.Enums.OrderStatus>(orderRow.Status, out var statusEnum))
            {
                cancellable = YummyZoom.Domain.OrderAggregate.Enums.OrderStatusExtensions.IsCancellable(statusEnum);
            }
        }
        catch
        {
            cancellable = false;
        }

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
            orderRow.Note,
            orderRow.Currency,
            orderRow.SubtotalAmount,
            orderRow.DiscountAmount,
            orderRow.DeliveryFeeAmount,
            orderRow.TipAmount,
            orderRow.TaxAmount,
            orderRow.TotalAmount,
            orderRow.AppliedCouponId,
            orderRow.SourceTeamCartId,
            orderRow.DeliveryAddress_Street,
            orderRow.DeliveryAddress_City,
            orderRow.DeliveryAddress_State,
            orderRow.DeliveryAddress_Country,
            orderRow.DeliveryAddress_PostalCode,
            itemDtos,
            orderRow.RestaurantName,
            orderRow.RestaurantAddress_Street,
            orderRow.RestaurantAddress_City,
            orderRow.RestaurantAddress_State,
            orderRow.RestaurantAddress_Country,
            orderRow.RestaurantAddress_PostalCode,
            orderRow.RestaurantLat,
            orderRow.RestaurantLon,
            null, // DeliveryLat not available currently
            null, // DeliveryLon not available currently
            null, // DistanceKm placeholder for future computation
            paymentMethod,
            cancellable,
            IsFromTeamCart: orderRow.SourceTeamCartId.HasValue,
            PaidOnlineAmount: paymentSplit.PaidOnlineAmount,
            CashOnDeliveryAmount: paymentSplit.CashOnDeliveryAmount);

        return Result.Success(details);
    }

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
        string? Note,
        string Currency,
        decimal SubtotalAmount,
        decimal DiscountAmount,
        decimal DeliveryFeeAmount,
        decimal TipAmount,
        decimal TaxAmount,
        decimal TotalAmount,
        Guid? AppliedCouponId,
        Guid? SourceTeamCartId,
        string? DeliveryAddress_Street,
        string? DeliveryAddress_City,
        string? DeliveryAddress_State,
        string? DeliveryAddress_Country,
        string? DeliveryAddress_PostalCode,
        string? RestaurantName,
        string? RestaurantAddress_Street,
        string? RestaurantAddress_City,
        string? RestaurantAddress_State,
        string? RestaurantAddress_Country,
        string? RestaurantAddress_PostalCode,
        double? RestaurantLat,
        double? RestaurantLon);

    private sealed record OrderItemRow(
        Guid OrderItemId,
        Guid MenuItemId,
        string Name,
        int Quantity,
        decimal UnitPriceAmount,
        decimal LineItemTotalAmount,
        string? SelectedCustomizations,
        string? ImageUrl);

    private sealed record PaymentSplitRow(decimal PaidOnlineAmount, decimal CashOnDeliveryAmount);
}
