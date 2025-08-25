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
    private readonly ILogger<OrderPaymentSucceededEventHandler> _logger;

    public OrderPaymentSucceededEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IOrderRepository orderRepository,
        IOrderRealtimeNotifier notifier,
        ILogger<OrderPaymentSucceededEventHandler> logger) : base(uow, inbox)
    {
        _orderRepository = orderRepository;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(OrderPaymentSucceeded notification, CancellationToken ct)
    {
        _logger.LogInformation("Handling OrderPaymentSucceeded (EventId={EventId}, OrderId={OrderId})", notification.EventId, notification.OrderId.Value);

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
            // Notify both restaurant and customer about payment success
            await _notifier.NotifyOrderPaymentSucceeded(dto, NotificationTarget.Both, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed broadcasting OrderPaymentSucceeded (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            throw; // allow retry
        }

        _logger.LogInformation("Handled OrderPaymentSucceeded (EventId={EventId}, OrderId={OrderId})", notification.EventId, notification.OrderId.Value);
    }
}
