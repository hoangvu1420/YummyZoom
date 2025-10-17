using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Handles ItemRemovedFromTeamCart events by removing the item from the Redis VM and notifying clients.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class ItemRemovedFromTeamCartEventHandler : IdempotentNotificationHandler<ItemRemovedFromTeamCart>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ILogger<ItemRemovedFromTeamCartEventHandler> _logger;

    public ItemRemovedFromTeamCartEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRealtimeNotifier notifier,
        ILogger<ItemRemovedFromTeamCartEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(ItemRemovedFromTeamCart notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogDebug("Handling ItemRemovedFromTeamCart (EventId={EventId}, CartId={CartId}, ItemId={ItemId})",
            notification.EventId, cartId.Value, notification.TeamCartItemId.Value);

        try
        {
            await _store.RemoveItemAsync(cartId, notification.TeamCartItemId.Value, ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove item from VM or notify (CartId={CartId}, ItemId={ItemId}, EventId={EventId})",
                cartId.Value, notification.TeamCartItemId.Value, notification.EventId);
            throw; // allow outbox retry
        }
    }
}

