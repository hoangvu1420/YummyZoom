using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.Common.Models;

namespace YummyZoom.Application.Orders.Broadcasting;

/// <summary>
/// Lightweight, versionable contract for pushing order lifecycle/payment updates to real-time clients.
/// Only immutable snapshot data (no behaviors). Fields kept minimal to reduce wire payload; can be extended later.
/// </summary>
public sealed record OrderStatusBroadcastDto(
    Guid OrderId,
    string OrderNumber,
    string Status,
    Guid RestaurantId,
    DateTime LastUpdateTimestamp,
    DateTime? EstimatedDeliveryTime,
    DateTime? ActualDeliveryTime,
    DateTime? DeliveredAt,
    DateTime OccurredOnUtc,
    Guid EventId
)
{
    /// <summary>
    /// Creates a generic lifecycle/status broadcast (non-delivery specific) from the current <see cref="Order"/> and its triggering <see cref="DomainEventBase"/>.
    /// </summary>
    public static OrderStatusBroadcastDto From(Order order, DomainEventBase @event) => new(
        order.Id.Value,
        order.OrderNumber,
        order.Status.ToString(),
        order.RestaurantId.Value,
        order.LastUpdateTimestamp,
        order.EstimatedDeliveryTime,
        order.ActualDeliveryTime,
        DeliveredAt: null,
        @event.OccurredOnUtc,
        @event.EventId);

    /// <summary>
    /// Creates a delivery-complete broadcast including the delivered timestamp.
    /// </summary>
    public static OrderStatusBroadcastDto Delivered(Order order, DomainEventBase deliveredEvent, DateTime deliveredAt) => new(
        order.Id.Value,
        order.OrderNumber,
        order.Status.ToString(),
        order.RestaurantId.Value,
        order.LastUpdateTimestamp,
        order.EstimatedDeliveryTime,
        order.ActualDeliveryTime,
        deliveredAt,
        deliveredEvent.OccurredOnUtc,
        deliveredEvent.EventId);
}
