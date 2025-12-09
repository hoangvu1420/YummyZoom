using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.TeamCarts.Models;
using YummyZoom.Domain.TeamCartAggregate.Events;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Handles ItemAddedToTeamCart events by adding an item snapshot to the Redis VM and notifying clients.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class ItemAddedToTeamCartEventHandler : IdempotentNotificationHandler<ItemAddedToTeamCart>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ITeamCartPushNotifier _pushNotifier;
    private readonly ILogger<ItemAddedToTeamCartEventHandler> _logger;

    public ItemAddedToTeamCartEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartRepository teamCartRepository,
        IMenuItemRepository menuItemRepository,
        ITeamCartStore store,
        ITeamCartRealtimeNotifier notifier,
        ITeamCartPushNotifier pushNotifier,
        ILogger<ItemAddedToTeamCartEventHandler> logger) : base(uow, inbox)
    {
        _teamCartRepository = teamCartRepository;
        _menuItemRepository = menuItemRepository;
        _store = store;
        _notifier = notifier;
        _pushNotifier = pushNotifier;
        _logger = logger;
    }

    protected override async Task HandleCore(ItemAddedToTeamCart notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogDebug("Handling ItemAddedToTeamCart (EventId={EventId}, CartId={CartId}, ItemId={ItemId})", notification.EventId, cartId.Value, notification.TeamCartItemId.Value);

        // Load aggregate with items to map snapshot fields
        var cart = await _teamCartRepository.GetByIdAsync(cartId, ct);
        if (cart is null)
        {
            _logger.LogWarning("ItemAddedToTeamCart handler could not find cart (CartId={CartId}, EventId={EventId})", cartId.Value, notification.EventId);
            return;
        }

        var item = cart.Items.FirstOrDefault(i => i.Id == notification.TeamCartItemId);
        if (item is null)
        {
            _logger.LogWarning("ItemAddedToTeamCart handler could not find item (CartId={CartId}, ItemId={ItemId}, EventId={EventId})", cartId.Value, notification.TeamCartItemId.Value, notification.EventId);
            return;
        }

        // Load MenuItem to get ImageUrl
        var menuItem = await _menuItemRepository.GetByIdAsync(item.Snapshot_MenuItemId, ct);
        if (menuItem is null)
        {
            _logger.LogWarning("ItemAddedToTeamCart handler could not find MenuItem (CartId={CartId}, ItemId={ItemId}, MenuItemId={MenuItemId}, EventId={EventId})", cartId.Value, notification.TeamCartItemId.Value, item.Snapshot_MenuItemId.Value, notification.EventId);
            // Continue with null ImageUrl if MenuItem not found (defensive)
        }

        var vmItem = new TeamCartViewModel.Item
        {
            ItemId = item.Id.Value,
            AddedByUserId = item.AddedByUserId.Value,
            Name = item.Snapshot_ItemName,
            ImageUrl = menuItem?.ImageUrl,
            MenuItemId = item.Snapshot_MenuItemId.Value,
            Quantity = item.Quantity,
            BasePrice = item.Snapshot_BasePriceAtOrder.Amount,
            LineTotal = item.LineItemTotal.Amount,
            Customizations = item.SelectedCustomizations.Select(c => new TeamCartViewModel.Customization
            {
                GroupName = c.Snapshot_CustomizationGroupName,
                ChoiceName = c.Snapshot_ChoiceName,
                PriceAdjustment = c.Snapshot_ChoicePriceAdjustmentAtOrder.Amount
            }).ToList()
        };

        try
        {
            await _store.AddItemAsync(cartId, vmItem, ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
            
            var vm = await _store.GetVmAsync(cartId, ct);
            if (vm is not null)
            {
                // Get actor name from VM members
                var actorMember = vm.Members.FirstOrDefault(m => m.UserId == item.AddedByUserId.Value);
                var actorName = actorMember?.Name ?? "Ai đó";
                
                // Notify others (data-only, no notification tray)
                var context = new TeamCartNotificationContext
                {
                    EventType = "ItemAdded",
                    ActorUserId = item.AddedByUserId.Value,
                    ActorName = actorName,
                    ItemName = item.Snapshot_ItemName,
                    Quantity = item.Quantity
                };
                
                var push = await _pushNotifier.PushTeamCartDataAsync(
                    cartId, 
                    vm.Version, 
                    TeamCartNotificationTarget.Others,
                    context,
                    NotificationDeliveryType.DataOnly,
                    ct);
                if (push.IsFailure)
                {
                    throw new InvalidOperationException(push.Error.Description);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add item to VM or notify (CartId={CartId}, ItemId={ItemId}, EventId={EventId})", cartId.Value, notification.TeamCartItemId.Value, notification.EventId);
            throw; // allow outbox retry
        }
    }
}

