using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.Orders.Broadcasting;
using YummyZoom.Domain.OrderAggregate.Events;

namespace YummyZoom.Application.Orders.EventHandlers;

/// <summary>
/// Handles OrderAccepted domain events.
/// Responsibilities: Transition visible state to Accepted; start kitchen prep SLA tracking; calculate and send ETA to the customer.
/// Side Effects: Broadcast status; (future) start prep SLA timer; (future) update order throughput metrics; (future) clear any acceptance reminder timer.
/// Exclusions: Still no revenue recognition.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class OrderAcceptedEventHandler : IdempotentNotificationHandler<OrderAccepted>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderRealtimeNotifier _notifier;
    private readonly ILogger<OrderAcceptedEventHandler> _logger;

    public OrderAcceptedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IOrderRepository orderRepository,
        IOrderRealtimeNotifier notifier,
        ILogger<OrderAcceptedEventHandler> logger) : base(uow, inbox)
    {
        _orderRepository = orderRepository;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(OrderAccepted notification, CancellationToken ct)
    {
        _logger.LogInformation("Handling OrderAccepted (EventId={EventId}, OrderId={OrderId})", notification.EventId, notification.OrderId.Value);

        var order = await _orderRepository.GetByIdAsync(notification.OrderId, ct);
        if (order is null)
        {
            _logger.LogWarning("OrderAccepted handler could not find order (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            return; // Idempotent record still written by base flow.
        }

        var dto = OrderStatusBroadcastDto.From(order, notification);

        try
        {
            await _notifier.NotifyOrderStatusChanged(dto, ct);
        }
        catch (Exception ex)
        {
            // Best-effort broadcast: allow retry via outbox (idempotent) by rethrowing.
            _logger.LogError(ex, "Failed broadcasting OrderAccepted (OrderId={OrderId}, EventId={EventId})", notification.OrderId.Value, notification.EventId);
            throw;
        }

        _logger.LogInformation("Handled OrderAccepted (EventId={EventId}, OrderId={OrderId})", notification.EventId, notification.OrderId.Value);
    }
}