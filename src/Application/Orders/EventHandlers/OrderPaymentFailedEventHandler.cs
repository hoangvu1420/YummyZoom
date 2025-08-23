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
    private readonly ILogger<OrderPaymentFailedEventHandler> _logger;

    public OrderPaymentFailedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IOrderRepository orderRepository,
        IOrderRealtimeNotifier notifier,
        ILogger<OrderPaymentFailedEventHandler> logger) : base(uow, inbox)
    {
        _orderRepository = orderRepository;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(OrderPaymentFailed notification, CancellationToken ct)
    {
        _logger.LogInformation("Handling OrderPaymentFailed (EventId={EventId}, OrderId={OrderId})", notification.EventId, notification.OrderId.Value);

        var order = await _orderRepository.GetByIdAsync(notification.OrderId, ct);
        if (order is null)
        {
            _logger.LogWarning("OrderPaymentFailed handler could not find order (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            return; // Idempotent record still written by base flow.
        }

        var dto = OrderStatusBroadcastDto.From(order, notification);

        try
        {
            await _notifier.NotifyOrderPaymentFailed(dto, ct);
        }
        catch (Exception ex)
        {
            // Best-effort broadcast: allow retry via outbox (idempotent) by rethrowing.
            _logger.LogError(ex, "Failed broadcasting OrderPaymentFailed (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            throw;
        }

        _logger.LogInformation("Handled OrderPaymentFailed (EventId={EventId}, OrderId={OrderId})", notification.EventId, notification.OrderId.Value);
    }
}
