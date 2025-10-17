using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.MenuItems.EventHandlers.Shared;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.MenuItemAggregate.Events;

namespace YummyZoom.Application.MenuItems.EventHandlers;

/// <summary>
/// Handles MenuItemAvailabilityChanged events by rebuilding the FullMenuView for the affected restaurant.
/// RestaurantId is resolved via repository as the event does not carry it.
/// </summary>
public sealed class MenuItemAvailabilityChangedEventHandler : MenuItemProjectorBase<MenuItemAvailabilityChanged>
{
    public MenuItemAvailabilityChangedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        IMenuItemRepository menuItemRepository,
        ILogger<MenuItemAvailabilityChangedEventHandler> logger)
        : base(uow, inbox, rebuilder, menuItemRepository, logger)
    { }

    protected override async Task HandleCore(MenuItemAvailabilityChanged notification, CancellationToken ct)
    {
        _logger.LogDebug("Handling MenuItemAvailabilityChanged (EventId={EventId}, MenuItemId={MenuItemId}, IsAvailable={IsAvailable})",
            notification.EventId, notification.MenuItemId.Value, notification.IsAvailable);

        var restaurantId = await ResolveRestaurantIdAsync(notification.MenuItemId, ct);
        if (restaurantId is null)
        {
            return; // logged by base method
        }

        await RebuildForRestaurantAsync(restaurantId.Value, ct);
    }
}

