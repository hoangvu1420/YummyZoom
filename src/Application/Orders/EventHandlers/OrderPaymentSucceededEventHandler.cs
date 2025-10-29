using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.Orders.Broadcasting;
using YummyZoom.Domain.OrderAggregate.Events;

namespace YummyZoom.Application.Orders.EventHandlers;

/// <summary>
/// Handles OrderPaymentSucceeded domain events.
/// Responsibilities (Phase 1): broadcast payment success lifecycle snapshot via realtime notifier.
/// Exclusions: no revenue recognition (deferred to OrderDelivered), no pending revenue tracking yet.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class OrderPaymentSucceededEventHandler : IdempotentNotificationHandler<OrderPaymentSucceeded>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderRealtimeNotifier _notifier;
    private readonly IOrderPushNotifier _orderPushNotifier;
    private readonly ILogger<OrderPaymentSucceededEventHandler> _logger;

    public OrderPaymentSucceededEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IOrderRepository orderRepository,
        IOrderRealtimeNotifier notifier,
        IOrderPushNotifier orderPushNotifier,
        ILogger<OrderPaymentSucceededEventHandler> logger) : base(uow, inbox)
    {
        _orderRepository = orderRepository;
        _notifier = notifier;
        _orderPushNotifier = orderPushNotifier;
        _logger = logger;
    }

    protected override async Task HandleCore(OrderPaymentSucceeded notification, CancellationToken ct)
    {
        _logger.LogDebug("Handling OrderPaymentSucceeded (EventId={EventId}, OrderId={OrderId})", notification.EventId, notification.OrderId.Value);

        var order = await _orderRepository.GetByIdAsync(notification.OrderId, ct);
        if (order is null)
        {
            _logger.LogWarning("OrderPaymentSucceeded handler could not find order (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            return; // Still idempotent record
        }

        // Build snapshot DTO (status should now be Placed)
        var dto = OrderStatusBroadcastDto.From(order, notification);

        try
        {
            // Notify both restaurant and customer via SignalR
            await _notifier.NotifyOrderPaymentSucceeded(dto, NotificationTarget.Both, ct);

            var push = await _orderPushNotifier.PushOrderDataAsync(order.Id.Value, order.CustomerId.Value, order.Version, ct);
            if (push.IsFailure)
            {
                throw new InvalidOperationException(push.Error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed broadcasting OrderPaymentSucceeded/FCM (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            throw; // allow retry
        }
    }
}
