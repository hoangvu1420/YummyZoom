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
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ITeamCartPushNotifier _pushNotifier;
    private readonly ILogger<ItemRemovedFromTeamCartEventHandler> _logger;

    public ItemRemovedFromTeamCartEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRepository teamCartRepository,
        ITeamCartRealtimeNotifier notifier,
        ITeamCartPushNotifier pushNotifier,
        ILogger<ItemRemovedFromTeamCartEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _teamCartRepository = teamCartRepository;
        _notifier = notifier;
        _pushNotifier = pushNotifier;
        _logger = logger;
    }

    protected override async Task HandleCore(ItemRemovedFromTeamCart notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogDebug("Handling ItemRemovedFromTeamCart (EventId={EventId}, CartId={CartId}, ItemId={ItemId})",
            notification.EventId, cartId.Value, notification.TeamCartItemId.Value);

        var cart = await _teamCartRepository.GetByIdAsync(cartId, ct);
        if (cart is null)
        {
            _logger.LogWarning("ItemRemovedFromTeamCart handler could not find cart (CartId={CartId}, EventId={EventId})", cartId.Value, notification.EventId);
            return;
        }

        try
        {
            await _store.RemoveItemAsync(cartId, notification.TeamCartItemId.Value, ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
            
            var vm = await _store.GetVmAsync(cartId, ct);
            if (vm is not null)
            {
                var push = await _pushNotifier.PushTeamCartDataAsync(cartId, vm.Version, ct);
                if (push.IsFailure)
                {
                    throw new InvalidOperationException(push.Error.Description);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove item from VM or notify (CartId={CartId}, ItemId={ItemId}, EventId={EventId})",
                cartId.Value, notification.TeamCartItemId.Value, notification.EventId);
            throw; // allow outbox retry
        }
    }
}

