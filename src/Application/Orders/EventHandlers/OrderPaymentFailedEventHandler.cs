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
    private readonly IUserDeviceSessionRepository _userDeviceSessionRepository;
    private readonly IFcmService _fcm;
    private readonly ILogger<OrderPaymentFailedEventHandler> _logger;

    public OrderPaymentFailedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IOrderRepository orderRepository,
        IOrderRealtimeNotifier notifier,
        IUserDeviceSessionRepository userDeviceSessionRepository,
        IFcmService fcm,
        ILogger<OrderPaymentFailedEventHandler> logger) : base(uow, inbox)
    {
        _orderRepository = orderRepository;
        _notifier = notifier;
        _userDeviceSessionRepository = userDeviceSessionRepository;
        _fcm = fcm;
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

            // Push data-only to customer devices: {orderId, version}
            var tokens = await _userDeviceSessionRepository.GetActiveFcmTokensByUserIdAsync(order.CustomerId.Value, ct);
            if (tokens.Count > 0)
            {
                var data = new Dictionary<string, string>
                {
                    ["orderId"] = order.Id.Value.ToString(),
                    ["version"] = order.Version.ToString()
                };
                var push = await _fcm.SendMulticastDataAsync(tokens, data);
                if (push.IsFailure)
                {
                    throw new InvalidOperationException(push.Error.Description);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed broadcasting OrderPaymentFailed/FCM (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            throw;
        }
    }
}
