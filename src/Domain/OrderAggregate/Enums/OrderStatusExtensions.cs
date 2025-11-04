using System;

namespace YummyZoom.Domain.OrderAggregate.Enums;

public static class OrderStatusExtensions
{
    public static bool IsCancellable(this OrderStatus status)
        => status == OrderStatus.AwaitingPayment || status == OrderStatus.Placed;
}

