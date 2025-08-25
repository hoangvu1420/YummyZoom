using YummyZoom.Application.Orders.Broadcasting;

namespace YummyZoom.Application.Common.Interfaces.IServices;

/// <summary>
/// Notification routing targets for order real-time notifications.
/// Supports targeting Restaurant, Customer, or Both audiences.
/// </summary>
public enum NotificationTarget
{
    /// <summary>
    /// Send notification to restaurant hub groups only.
    /// </summary>
    Restaurant = 1,
    
    /// <summary>
    /// Send notification to customer hub groups only.
    /// </summary>
    Customer = 2,
    
    /// <summary>
    /// Send notification to both restaurant and customer hub groups.
    /// </summary>
    Both = Restaurant | Customer
}

/// <summary>
/// Abstraction for pushing real-time order lifecycle/payment updates.
/// Initial implementation is a no-op; replaced later with SignalR hub adapter.
/// </summary>
public interface IOrderRealtimeNotifier
{
    Task NotifyOrderPlaced(OrderStatusBroadcastDto dto, NotificationTarget target = NotificationTarget.Restaurant, CancellationToken cancellationToken = default);
    Task NotifyOrderPaymentSucceeded(OrderStatusBroadcastDto dto, NotificationTarget target = NotificationTarget.Restaurant, CancellationToken cancellationToken = default);
    Task NotifyOrderPaymentFailed(OrderStatusBroadcastDto dto, NotificationTarget target = NotificationTarget.Restaurant, CancellationToken cancellationToken = default);
    Task NotifyOrderStatusChanged(OrderStatusBroadcastDto dto, NotificationTarget target = NotificationTarget.Restaurant, CancellationToken cancellationToken = default);
}
