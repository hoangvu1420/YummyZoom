using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.MenuItems.EventHandlers.Shared;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.MenuItemAggregate.Events;

namespace YummyZoom.Application.MenuItems.EventHandlers;

public sealed class MenuItemAssignedToCategoryEventHandler : MenuItemProjectorBase<MenuItemAssignedToCategory>
{
    public MenuItemAssignedToCategoryEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        IMenuItemRepository menuItemRepository,
        ILogger<MenuItemAssignedToCategoryEventHandler> logger)
        : base(uow, inbox, rebuilder, menuItemRepository, logger)
    { }

    protected override async Task HandleCore(MenuItemAssignedToCategory notification, CancellationToken ct)
    {
        var restaurantId = await ResolveRestaurantIdAsync(notification.MenuItemId, ct);
        if (restaurantId is null) return;

        await RebuildForRestaurantAsync(restaurantId.Value, ct);
    }
}

