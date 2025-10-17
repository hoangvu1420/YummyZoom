using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.MenuItems.EventHandlers.Shared;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.MenuItemAggregate.Events;

namespace YummyZoom.Application.MenuItems.EventHandlers;

public sealed class MenuItemCustomizationRemovedEventHandler : MenuItemProjectorBase<MenuItemCustomizationRemoved>
{
    public MenuItemCustomizationRemovedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        IMenuItemRepository menuItemRepository,
        ILogger<MenuItemCustomizationRemovedEventHandler> logger)
        : base(uow, inbox, rebuilder, menuItemRepository, logger)
    { }

    protected override async Task HandleCore(MenuItemCustomizationRemoved notification, CancellationToken ct)
    {
        _logger.LogDebug("Handling MenuItemCustomizationRemoved (EventId={EventId}, MenuItemId={MenuItemId}, CustomizationGroupId={CustomizationGroupId})",
            notification.EventId, notification.MenuItemId.Value, notification.CustomizationGroupId.Value);
        
        var restaurantId = await ResolveRestaurantIdAsync(notification.MenuItemId, ct);
        if (restaurantId is null) return;

        await RebuildForRestaurantAsync(restaurantId.Value, ct);
    }
}

