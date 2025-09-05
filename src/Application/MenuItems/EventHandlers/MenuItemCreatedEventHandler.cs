using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.MenuItems.EventHandlers.Shared;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.MenuItemAggregate.Events;

namespace YummyZoom.Application.MenuItems.EventHandlers;

/// <summary>
/// Handles MenuItemCreated domain events by rebuilding the FullMenuView for the affected restaurant.
/// Strategy: naive, correct-first full rebuild per restaurant.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class MenuItemCreatedEventHandler : MenuItemProjectorBase<MenuItemCreated>
{
    public MenuItemCreatedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        IMenuItemRepository menuItemRepository,
        ILogger<MenuItemCreatedEventHandler> logger)
        : base(uow, inbox, rebuilder, menuItemRepository, logger)
    { }

    protected override async Task HandleCore(MenuItemCreated notification, CancellationToken ct)
    {
        _logger.LogInformation("Handling MenuItemCreated (EventId={EventId}, MenuItemId={MenuItemId}, RestaurantId={RestaurantId})",
            notification.EventId, notification.MenuItemId.Value, notification.RestaurantId.Value);

        await RebuildForRestaurantAsync(notification.RestaurantId.Value, ct);
    }
}

