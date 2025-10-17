using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.Orders.Broadcasting;
using YummyZoom.Domain.OrderAggregate.Events;

namespace YummyZoom.Application.Orders.EventHandlers;

/// <summary>
/// Handles OrderReadyForDelivery domain events by notifying real-time clients about the order status change.
/// Responsibilities: Indicate order is ready for pickup/delivery dispatch; adjust ETA for delivery.
/// Side Effects: Broadcast status; (future) notify delivery assignment subsystem; (future) start ready-to-delivered SLA timer.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class OrderReadyForDeliveryEventHandler : IdempotentNotificationHandler<OrderReadyForDelivery>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderRealtimeNotifier _notifier;
    private readonly ILogger<OrderReadyForDeliveryEventHandler> _logger;

    public OrderReadyForDeliveryEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IOrderRepository orderRepository,
        IOrderRealtimeNotifier notifier,
        ILogger<OrderReadyForDeliveryEventHandler> logger) : base(uow, inbox)
    {
        _orderRepository = orderRepository;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(OrderReadyForDelivery notification, CancellationToken ct)
    {
        _logger.LogDebug("Handling OrderReadyForDelivery (EventId={EventId}, OrderId={OrderId})", notification.EventId, notification.OrderId.Value);

        var order = await _orderRepository.GetByIdAsync(notification.OrderId, ct);
        if (order is null)
        {
            _logger.LogWarning("OrderReadyForDelivery handler could not find order (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            return; // Idempotent record still written by base flow.
        }

        var dto = OrderStatusBroadcastDto.From(order, notification);

        try
        {
            // Notify customer that order is ready for delivery (restaurant already knows they marked it ready)
            await _notifier.NotifyOrderStatusChanged(dto, NotificationTarget.Customer, ct);
        }
        catch (Exception ex)
        {
            // Best-effort broadcast: allow retry via outbox (idempotent) by rethrowing.
            _logger.LogError(ex, "Failed broadcasting OrderReadyForDelivery (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            throw;
        }
    }
}
