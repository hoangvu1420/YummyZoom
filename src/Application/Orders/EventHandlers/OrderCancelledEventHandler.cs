using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.Orders.Broadcasting;
using YummyZoom.Domain.OrderAggregate.Events;

namespace YummyZoom.Application.Orders.EventHandlers;

/// <summary>
/// Handles OrderCancelled domain events.
/// Responsibilities: Reflect user/staff cancellation; broadcast status change to clients.
/// Exclusions: No revenue recognition; refund logic deferred.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class OrderCancelledEventHandler : IdempotentNotificationHandler<OrderCancelled>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderRealtimeNotifier _notifier;
    private readonly ILogger<OrderCancelledEventHandler> _logger;

    public OrderCancelledEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IOrderRepository orderRepository,
        IOrderRealtimeNotifier notifier,
        ILogger<OrderCancelledEventHandler> logger) : base(uow, inbox)
    {
        _orderRepository = orderRepository;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(OrderCancelled notification, CancellationToken ct)
    {
        _logger.LogDebug("Handling OrderCancelled (EventId={EventId}, OrderId={OrderId})", notification.EventId, notification.OrderId.Value);

        var order = await _orderRepository.GetByIdAsync(notification.OrderId, ct);
        if (order is null)
        {
            _logger.LogWarning("OrderCancelled handler could not find order (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            return; // Idempotent record still written by base flow.
        }

        var dto = OrderStatusBroadcastDto.From(order, notification);

        try
        {
            // Notify restaurant that order has been cancelled (customer already knows they cancelled it)
            await _notifier.NotifyOrderStatusChanged(dto, NotificationTarget.Restaurant, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed broadcasting OrderCancelled (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            throw; // allow retry
        }
    }
}
