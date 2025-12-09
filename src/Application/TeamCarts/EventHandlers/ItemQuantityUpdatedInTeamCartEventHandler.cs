using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Handles ItemQuantityUpdatedInTeamCart events by updating the item's quantity in the Redis VM and notifying clients.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class ItemQuantityUpdatedInTeamCartEventHandler : IdempotentNotificationHandler<ItemQuantityUpdatedInTeamCart>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ITeamCartPushNotifier _pushNotifier;
    private readonly ILogger<ItemQuantityUpdatedInTeamCartEventHandler> _logger;

    public ItemQuantityUpdatedInTeamCartEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRepository teamCartRepository,
        ITeamCartRealtimeNotifier notifier,
        ITeamCartPushNotifier pushNotifier,
        ILogger<ItemQuantityUpdatedInTeamCartEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _teamCartRepository = teamCartRepository;
        _notifier = notifier;
        _pushNotifier = pushNotifier;
        _logger = logger;
    }

    protected override async Task HandleCore(ItemQuantityUpdatedInTeamCart notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogDebug(
            "Handling ItemQuantityUpdatedInTeamCart (EventId={EventId}, CartId={CartId}, ItemId={ItemId}, OldQty={OldQty}, NewQty={NewQty})",
            notification.EventId, cartId.Value, notification.TeamCartItemId.Value, notification.OldQuantity, notification.NewQuantity);

        var cart = await _teamCartRepository.GetByIdAsync(cartId, ct);
        if (cart is null)
        {
            _logger.LogWarning("ItemQuantityUpdatedInTeamCart handler could not find cart (CartId={CartId}, EventId={EventId})", cartId.Value, notification.EventId);
            return;
        }

        try
        {
            await _store.UpdateItemQuantityAsync(cartId, notification.TeamCartItemId.Value, notification.NewQuantity, ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
            
            // Suppress push notification for ItemQuantityUpdated (low-value event)
            // Quantity changes are frequent and don't require immediate push notifications
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update item qty in VM or notify (CartId={CartId}, ItemId={ItemId}, EventId={EventId})",
                cartId.Value, notification.TeamCartItemId.Value, notification.EventId);
            throw; // allow outbox retry
        }
    }
}

