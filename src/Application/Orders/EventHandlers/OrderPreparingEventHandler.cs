using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.Orders.Broadcasting;
using YummyZoom.Domain.OrderAggregate.Events;

namespace YummyZoom.Application.Orders.EventHandlers;

/// <summary>
/// Handles OrderPreparing domain events by notifying real-time clients about the order status change.
/// Responsibilities: Broadcast status change to restaurant dashboards and customer interfaces.
/// Side Effects: Real-time notification broadcast.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class OrderPreparingEventHandler : IdempotentNotificationHandler<OrderPreparing>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderRealtimeNotifier _notifier;
    private readonly IUserDeviceSessionRepository _userDeviceSessionRepository;
    private readonly IFcmService _fcm;
    private readonly ILogger<OrderPreparingEventHandler> _logger;

    public OrderPreparingEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IOrderRepository orderRepository,
        IOrderRealtimeNotifier notifier,
        IUserDeviceSessionRepository userDeviceSessionRepository,
        IFcmService fcm,
        ILogger<OrderPreparingEventHandler> logger) : base(uow, inbox)
    {
        _orderRepository = orderRepository;
        _notifier = notifier;
        _userDeviceSessionRepository = userDeviceSessionRepository;
        _fcm = fcm;
        _logger = logger;
    }

    protected override async Task HandleCore(OrderPreparing notification, CancellationToken ct)
    {
        _logger.LogDebug("Handling OrderPreparing (EventId={EventId}, OrderId={OrderId})", notification.EventId, notification.OrderId.Value);

        var order = await _orderRepository.GetByIdAsync(notification.OrderId, ct);
        if (order is null)
        {
            _logger.LogWarning("OrderPreparing handler could not find order (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            return; // Idempotent record still written by base flow.
        }

        var dto = OrderStatusBroadcastDto.From(order, notification);

        try
        {
            // Notify customer via SignalR
            await _notifier.NotifyOrderStatusChanged(dto, NotificationTarget.Customer, ct);

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
            _logger.LogError(ex, "Failed broadcasting OrderPreparing/FCM (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            throw;
        }
    }
}
