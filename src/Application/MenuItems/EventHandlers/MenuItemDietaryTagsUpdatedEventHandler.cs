using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.MenuItems.EventHandlers.Shared;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.MenuItemAggregate.Events;

namespace YummyZoom.Application.MenuItems.EventHandlers;

public sealed class MenuItemDietaryTagsUpdatedEventHandler : MenuItemProjectorBase<MenuItemDietaryTagsUpdated>
{
    public MenuItemDietaryTagsUpdatedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IMenuReadModelRebuilder rebuilder,
        IMenuItemRepository menuItemRepository,
        ILogger<MenuItemDietaryTagsUpdatedEventHandler> logger)
        : base(uow, inbox, rebuilder, menuItemRepository, logger)
    { }

    protected override async Task HandleCore(MenuItemDietaryTagsUpdated notification, CancellationToken ct)
    {
        var restaurantId = await ResolveRestaurantIdAsync(notification.MenuItemId, ct);
        if (restaurantId is null) return;

        await RebuildForRestaurantAsync(restaurantId.Value, ct);
    }
}

