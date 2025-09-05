using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.MenuItems.EventHandlers.Shared;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.MenuItemAggregate.Events;

namespace YummyZoom.Application.MenuItems.EventHandlers;

/// <summary>
/// Handles MenuItemDetailsUpdated events by rebuilding the FullMenuView for the affected restaurant.
/// </summary>
public sealed class MenuItemDetailsUpdatedEventHandler : MenuItemProjectorBase<MenuItemDetailsUpdated>
{
    public MenuItemDetailsUpdatedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        IMenuItemRepository menuItemRepository,
        ILogger<MenuItemDetailsUpdatedEventHandler> logger)
        : base(uow, inbox, rebuilder, menuItemRepository, logger)
    { }

    protected override async Task HandleCore(MenuItemDetailsUpdated notification, CancellationToken ct)
    {
        var restaurantId = await ResolveRestaurantIdAsync(notification.MenuItemId, ct);
        if (restaurantId is null) return;

        await RebuildForRestaurantAsync(restaurantId.Value, ct);
    }
}

