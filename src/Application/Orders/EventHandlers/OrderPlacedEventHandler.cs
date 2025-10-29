using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.Orders.Broadcasting;
using YummyZoom.Domain.OrderAggregate.Events;

namespace YummyZoom.Application.Orders.EventHandlers;

/// <summary>
/// Handles OrderPlaced domain events: broadcasts a newly actionable order to restaurant dashboards.
/// No revenue or payment side-effects. Idempotent via inbox infrastructure.
/// </summary>
public sealed class OrderPlacedEventHandler : IdempotentNotificationHandler<OrderPlaced>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderRealtimeNotifier _notifier;
    private readonly IOrderPushNotifier _orderPushNotifier;
    private readonly ILogger<OrderPlacedEventHandler> _logger;

    public OrderPlacedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IOrderRepository orderRepository,
        IOrderRealtimeNotifier notifier,
        IOrderPushNotifier orderPushNotifier,
        ILogger<OrderPlacedEventHandler> logger) : base(uow, inbox)
    {
        _orderRepository = orderRepository;
        _notifier = notifier;
        _orderPushNotifier = orderPushNotifier;
        _logger = logger;
    }

    protected override async Task HandleCore(OrderPlaced notification, CancellationToken ct)
    {
        if (notification.OrderId == null)
        {
            return;
        }

        _logger.LogDebug("Handling OrderPlaced (EventId={EventId}, OrderId={OrderId})", notification.EventId, notification.OrderId.Value);

        var order = await _orderRepository.GetByIdAsync(notification.OrderId, ct);
        if (order is null)
        {
            _logger.LogWarning("OrderPlaced handler could not find order (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            return; // Idempotent record still written by base flow.
        }

        var dto = OrderStatusBroadcastDto.From(order, notification);

        try
        {
            // Notify restaurant via SignalR
            await _notifier.NotifyOrderPlaced(dto, NotificationTarget.Restaurant, ct);

            var push = await _orderPushNotifier.PushOrderDataAsync(order.Id.Value, order.CustomerId.Value, order.Version, ct);
            if (push.IsFailure)
            {
                throw new InvalidOperationException(push.Error.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed broadcasting OrderPlaced/FCM (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            throw;
        }
    }
}
