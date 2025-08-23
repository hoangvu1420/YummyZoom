using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Orders.Broadcasting;

namespace YummyZoom.Infrastructure.Realtime;

/// <summary>
/// No-op implementation used until SignalR (or other transport) is introduced.
/// </summary>
public class NoOpOrderRealtimeNotifier : IOrderRealtimeNotifier
{
    public Task NotifyOrderPlaced(OrderStatusBroadcastDto dto, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyOrderPaymentSucceeded(OrderStatusBroadcastDto dto, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyOrderPaymentFailed(OrderStatusBroadcastDto dto, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyOrderStatusChanged(OrderStatusBroadcastDto dto, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
