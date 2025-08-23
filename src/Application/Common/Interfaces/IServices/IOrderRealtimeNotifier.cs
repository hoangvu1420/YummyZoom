using YummyZoom.Application.Orders.Broadcasting;

namespace YummyZoom.Application.Common.Interfaces.IServices;

/// <summary>
/// Abstraction for pushing real-time order lifecycle/payment updates.
/// Initial implementation is a no-op; replaced later with SignalR hub adapter.
/// </summary>
public interface IOrderRealtimeNotifier
{
    Task NotifyOrderPlaced(OrderStatusBroadcastDto dto, CancellationToken cancellationToken = default);
    Task NotifyOrderPaymentSucceeded(OrderStatusBroadcastDto dto, CancellationToken cancellationToken = default);
    Task NotifyOrderPaymentFailed(OrderStatusBroadcastDto dto, CancellationToken cancellationToken = default);
    Task NotifyOrderStatusChanged(OrderStatusBroadcastDto dto, CancellationToken cancellationToken = default);
}
