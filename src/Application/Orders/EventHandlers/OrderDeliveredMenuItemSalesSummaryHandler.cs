using System.Linq;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Events;

namespace YummyZoom.Application.Orders.EventHandlers;

/// <summary>
/// Updates the MenuItemSalesSummary read model whenever an order is delivered.
/// Aggregates quantities per menu item and pushes incremental deltas into the summary table.
/// </summary>
public sealed class OrderDeliveredMenuItemSalesSummaryHandler : IdempotentNotificationHandler<OrderDelivered>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMenuItemSalesSummaryMaintainer _maintainer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OrderDeliveredMenuItemSalesSummaryHandler> _logger;

    public OrderDeliveredMenuItemSalesSummaryHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IOrderRepository orderRepository,
        IMenuItemSalesSummaryMaintainer maintainer,
        TimeProvider timeProvider,
        ILogger<OrderDeliveredMenuItemSalesSummaryHandler> logger) : base(uow, inbox)
    {
        _orderRepository = orderRepository;
        _maintainer = maintainer;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task HandleCore(OrderDelivered notification, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(notification.OrderId, ct);
        if (order is null)
        {
            _logger.LogWarning("MenuItemSalesSummary handler could not find order {OrderId}", notification.OrderId.Value);
            return;
        }

        if (order.Status != OrderStatus.Delivered)
        {
            _logger.LogDebug("Skipping sales summary update for order {OrderId} with status {Status}", order.Id.Value, order.Status);
            return;
        }

        var deliveredAtUtc = DateTime.SpecifyKind(notification.DeliveredAt, DateTimeKind.Utc);
        var deliveredAtOffset = new DateTimeOffset(deliveredAtUtc, TimeSpan.Zero);

        var now = _timeProvider.GetUtcNow();
        var sevenDayThreshold = now - TimeSpan.FromDays(7);
        var thirtyDayThreshold = now - TimeSpan.FromDays(30);

        var lifetimeQualifier = order.OrderItems
            .GroupBy(item => item.Snapshot_MenuItemId.Value)
            .Select(group => new
            {
                MenuItemId = group.Key,
                Quantity = group.Sum(i => (long)i.Quantity)
            })
            .ToList();

        foreach (var entry in lifetimeQualifier)
        {
            var lifetimeDelta = entry.Quantity;
            var rolling7Delta = deliveredAtOffset >= sevenDayThreshold ? entry.Quantity : 0;
            var rolling30Delta = deliveredAtOffset >= thirtyDayThreshold ? entry.Quantity : 0;

            await _maintainer.UpsertDeltaAsync(
                order.RestaurantId.Value,
                entry.MenuItemId,
                lifetimeDelta,
                rolling7Delta,
                rolling30Delta,
                deliveredAtOffset,
                deliveredAtOffset.UtcTicks,
                ct);
        }

        _logger.LogDebug("Updated MenuItemSalesSummaries for delivered order {OrderId}", order.Id.Value);
    }
}
