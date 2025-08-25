using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.Orders.Broadcasting;
using YummyZoom.Domain.OrderAggregate.Events;

namespace YummyZoom.Application.Orders.EventHandlers;

/// <summary>
/// Handles OrderDelivered domain events.
/// Responsibilities: finalize order lifecycle; recognize revenue; broadcast delivered status.
/// Side Effects: Call RestaurantAccount.RecordRevenue (recognition); broadcast delivered status; 
/// (future) convert pending revenue → recognized; (future) trigger loyalty points awarding; 
/// (future) emit customer feedback request event; (future) close any outstanding SLA timers.
/// Exclusions: No further status transitions allowed after broadcast.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class OrderDeliveredEventHandler : IdempotentNotificationHandler<OrderDelivered>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IRestaurantAccountRepository _restaurantAccountRepository;
    private readonly IOrderRealtimeNotifier _notifier;
    private readonly ILogger<OrderDeliveredEventHandler> _logger;

    public OrderDeliveredEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IOrderRepository orderRepository,
        IRestaurantAccountRepository restaurantAccountRepository,
        IOrderRealtimeNotifier notifier,
        ILogger<OrderDeliveredEventHandler> logger) : base(uow, inbox)
    {
        _orderRepository = orderRepository;
        _restaurantAccountRepository = restaurantAccountRepository;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(OrderDelivered notification, CancellationToken ct)
    {
        _logger.LogInformation("Handling OrderDelivered (EventId={EventId}, OrderId={OrderId})", 
            notification.EventId, notification.OrderId.Value);

        var order = await _orderRepository.GetByIdAsync(notification.OrderId, ct);
        if (order is null)
        {
            _logger.LogWarning("OrderDelivered handler could not find order (OrderId={OrderId}, EventId={EventId})", 
                notification.OrderId.Value, notification.EventId);
            return; // Still idempotent record
        }

        // Record revenue recognition
        await RecordRevenueAsync(order, ct);

        // Build and broadcast delivery status
        var dto = OrderStatusBroadcastDto.Delivered(order, notification, notification.DeliveredAt);

        try
        {
            // Notify both restaurant and customer that order has been delivered
            await _notifier.NotifyOrderStatusChanged(dto, NotificationTarget.Both, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed broadcasting OrderDelivered (OrderId={OrderId}, EventId={EventId})", 
                notification.OrderId.Value, notification.EventId);
            throw; // allow retry
        }

        _logger.LogInformation("Handled OrderDelivered (EventId={EventId}, OrderId={OrderId})", 
            notification.EventId, notification.OrderId.Value);
    }

    private async Task RecordRevenueAsync(Domain.OrderAggregate.Order order, CancellationToken ct)
    {
        try
        {
            // Use atomic get-or-create - no race condition possible
            var restaurantAccount = await _restaurantAccountRepository.GetOrCreateAsync(order.RestaurantId, ct);

            // Record revenue (gross amount for now; platform fee TODO)
            var revenueResult = restaurantAccount.RecordRevenue(order.TotalAmount, order.Id);
            if (revenueResult.IsFailure)
            {
                _logger.LogError("Failed to record revenue for order {OrderId}: {Error}", 
                    order.Id.Value, revenueResult.Error);
                throw new InvalidOperationException($"Failed to record revenue: {revenueResult.Error}");
            }

            await _restaurantAccountRepository.UpdateAsync(restaurantAccount, ct);
            
            _logger.LogInformation("Recorded revenue {Amount} for order {OrderId} to restaurant account {RestaurantId}", 
                order.TotalAmount.Amount, order.Id.Value, order.RestaurantId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record revenue for order {OrderId}", order.Id.Value);
            throw;
        }
    }
}
