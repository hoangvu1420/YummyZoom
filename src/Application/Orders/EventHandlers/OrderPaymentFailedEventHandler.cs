using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.Orders.Broadcasting;
using YummyZoom.Domain.OrderAggregate.Events;

namespace YummyZoom.Application.Orders.EventHandlers;

/// <summary>
/// Handles OrderPaymentFailed domain events: broadcasts payment failure information to restaurant dashboards.
/// Informs restaurant staff to ignore failed payment orders; enables customer re-payment flow.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class OrderPaymentFailedEventHandler : IdempotentNotificationHandler<OrderPaymentFailed>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderRealtimeNotifier _notifier;
    private readonly IOrderPushNotifier _orderPushNotifier;
    private readonly ILogger<OrderPaymentFailedEventHandler> _logger;

    public OrderPaymentFailedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IOrderRepository orderRepository,
        IOrderRealtimeNotifier notifier,
        IOrderPushNotifier orderPushNotifier,
        ILogger<OrderPaymentFailedEventHandler> logger) : base(uow, inbox)
    {
        _orderRepository = orderRepository;
        _notifier = notifier;
        _orderPushNotifier = orderPushNotifier;
        _logger = logger;
    }

    protected override async Task HandleCore(OrderPaymentFailed notification, CancellationToken ct)
    {
        _logger.LogDebug("Handling OrderPaymentFailed (EventId={EventId}, OrderId={OrderId})", notification.EventId, notification.OrderId.Value);

        var order = await _orderRepository.GetByIdAsync(notification.OrderId, ct);
        if (order is null)
        {
            _logger.LogWarning("OrderPaymentFailed handler could not find order (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            return; // Idempotent record still written by base flow.
        }

        var dto = OrderStatusBroadcastDto.From(order, notification);

        try
        {
            // Notify customer via SignalR
            await _notifier.NotifyOrderPaymentFailed(dto, NotificationTarget.Customer, ct);

            var push = await _orderPushNotifier.PushOrderDataAsync(order.Id.Value, order.CustomerId.Value, order.Version, ct);
            if (push.IsFailure)
            {
                throw new InvalidOperationException(push.Error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed broadcasting OrderPaymentFailed/FCM (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            throw;
        }
    }
}
