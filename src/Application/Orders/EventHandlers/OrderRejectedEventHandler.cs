using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.Orders.Broadcasting;
using YummyZoom.Domain.OrderAggregate.Events;

namespace YummyZoom.Application.Orders.EventHandlers;

/// <summary>
/// Handles OrderRejected domain events.
/// Responsibilities: Inform customer & restaurant dashboards that order was rejected early.
/// Side Effects: Broadcast status via realtime notifier. Future: (future) release pending revenue (if any); (future) restore coupon usage if business rules allow.
/// Exclusions: No revenue recognition or refunds here (future scope).
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class OrderRejectedEventHandler : IdempotentNotificationHandler<OrderRejected>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderRealtimeNotifier _notifier;
    private readonly IOrderPushNotifier _orderPushNotifier;
    private readonly ILogger<OrderRejectedEventHandler> _logger;

    public OrderRejectedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IOrderRepository orderRepository,
        IOrderRealtimeNotifier notifier,
        IOrderPushNotifier orderPushNotifier,
        ILogger<OrderRejectedEventHandler> logger) : base(uow, inbox)
    {
        _orderRepository = orderRepository;
        _notifier = notifier;
        _orderPushNotifier = orderPushNotifier;
        _logger = logger;
    }

    protected override async Task HandleCore(OrderRejected notification, CancellationToken ct)
    {
        _logger.LogDebug("Handling OrderRejected (EventId={EventId}, OrderId={OrderId})", notification.EventId, notification.OrderId.Value);

        var order = await _orderRepository.GetByIdAsync(notification.OrderId, ct);
        if (order is null)
        {
            _logger.LogWarning("OrderRejected handler could not find order (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            return; // Idempotent record still written by base flow.
        }

        var dto = OrderStatusBroadcastDto.From(order, notification);

        try
        {
            // Notify customer via SignalR
            await _notifier.NotifyOrderStatusChanged(dto, NotificationTarget.Customer, ct);

            var push = await _orderPushNotifier.PushOrderDataAsync(order.Id.Value, order.CustomerId.Value, order.Version, ct);
            if (push.IsFailure)
            {
                throw new InvalidOperationException(push.Error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed broadcasting OrderRejected/FCM (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            throw;
        }
    }
}
