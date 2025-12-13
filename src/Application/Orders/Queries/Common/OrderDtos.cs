using System.Text.Json;
using System.Text.Json.Serialization;

namespace YummyZoom.Application.Orders.Queries.Common;

/// <summary>
/// Lightweight projection used in paginated list queries for customer and restaurant order lists.
/// Keep fields minimal for performance; enrich via detail query when needed.
/// </summary>
public record OrderSummaryDto(
    Guid OrderId,
    string OrderNumber,
    string Status,
    DateTime PlacementTimestamp,
    Guid RestaurantId,
    string RestaurantName,
    string? RestaurantImageUrl,
    Guid CustomerId,
    decimal TotalAmount,
    string TotalCurrency,
    int ItemCount);

/// <summary>
/// Detailed order representation including monetary breakdown and line items.
/// </summary>
public record OrderDetailsDto(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    Guid RestaurantId,
    string Status,
    DateTime PlacementTimestamp,
    DateTime LastUpdateTimestamp,
    DateTime? EstimatedDeliveryTime,
    DateTime? ActualDeliveryTime,
    // Monetary breakdown
    string Currency, // All monetary values below share this currency
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal DeliveryFeeAmount,
    decimal TipAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    // Optional references
    Guid? AppliedCouponId,
    Guid? SourceTeamCartId,
    // Address snapshot (flattened)
    string? DeliveryAddress_Street,
    string? DeliveryAddress_City,
    string? DeliveryAddress_State,
    string? DeliveryAddress_Country,
    string? DeliveryAddress_PostalCode,
    IReadOnlyList<OrderItemDto> Items,
    // Restaurant snapshot (live read for now)
    string? RestaurantName,
    string? RestaurantAddress_Street,
    string? RestaurantAddress_City,
    string? RestaurantAddress_State,
    string? RestaurantAddress_Country,
    string? RestaurantAddress_PostalCode,
    // Geo info
    double? RestaurantLat,
    double? RestaurantLon,
    double? DeliveryLat,
    double? DeliveryLon,
    double? DistanceKm,
    // Payment & cancellation UX
    string? PaymentMethod,
    bool Cancellable);

/// <summary>
/// Line item snapshot at time of order placement.
/// </summary>
public record OrderItemDto(
    Guid OrderItemId,
    Guid MenuItemId,
    string Name,
    int Quantity,
    decimal UnitPriceAmount,
    decimal LineItemTotalAmount,
    IReadOnlyList<OrderItemCustomizationDto> Customizations,
    string? ImageUrl);

/// <summary>
/// Customization applied to an order item.
/// </summary>
public record OrderItemCustomizationDto(
    string GroupName,
    string ChoiceName,
    decimal? PriceAdjustmentAmount);

/// <summary>
/// Lean order status projection useful for polling clients.
/// </summary>
public record OrderStatusDto(
    Guid OrderId,
    string Status,
    DateTime LastUpdateTimestamp,
    DateTime? EstimatedDeliveryTime,
    long Version);

/// <summary>
/// Helper for parsing persisted JSON for SelectedCustomizations -> list of OrderItemCustomizationDto.
/// This keeps JSON parsing local to query handlers (no domain leakage).
/// </summary>
public static class OrderCustomizationJsonParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private record RawCustomization(
        string? Snapshot_CustomizationGroupName,
        string? Snapshot_ChoiceName,
        RawMoney? Snapshot_ChoicePriceAdjustmentAtOrder);

    private record RawMoney(decimal? Amount, string? Currency);

    public static IReadOnlyList<OrderItemCustomizationDto> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<OrderItemCustomizationDto>();
        try
        {
            var raw = JsonSerializer.Deserialize<List<RawCustomization>>(json, Options) ?? new();
            return raw
                .Where(r => r is { Snapshot_CustomizationGroupName: not null, Snapshot_ChoiceName: not null })
                .Select(r => new OrderItemCustomizationDto(
                    r.Snapshot_CustomizationGroupName!,
                    r.Snapshot_ChoiceName!,
                    r.Snapshot_ChoicePriceAdjustmentAtOrder?.Amount))
                .ToList();
        }
        catch
        {
            // Intentionally swallow & fallback to empty list to avoid failing whole query due to malformed legacy JSON.
            return Array.Empty<OrderItemCustomizationDto>();
        }
    }
}
