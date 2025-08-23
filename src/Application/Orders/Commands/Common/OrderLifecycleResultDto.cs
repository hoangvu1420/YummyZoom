using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Application.Orders.Commands.Common;

public sealed record OrderLifecycleResultDto(
    OrderId OrderId,
    string OrderNumber,
    string Status,
    DateTime PlacementTimestamp,
    DateTime LastUpdateTimestamp,
    DateTime? EstimatedDeliveryTime,
    DateTime? ActualDeliveryTime
);

public static class OrderLifecycleMappings
{
    public static OrderLifecycleResultDto ToLifecycleDto(this Order order) => new(
        order.Id,
        order.OrderNumber,
        order.Status.ToString(),
        order.PlacementTimestamp,
        order.LastUpdateTimestamp,
        order.EstimatedDeliveryTime,
        order.ActualDeliveryTime);
}
