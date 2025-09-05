using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.MenuEntity.Events;

namespace YummyZoom.Application.Menus.EventHandlers;

/// <summary>
/// Handles MenuRemoved domain events by deleting the FullMenuView for the affected restaurant.
/// Strategy: delete the restaurant's FullMenuView. Idempotent via inbox infrastructure.
/// </summary>
public sealed class MenuRemovedEventHandler : IdempotentNotificationHandler<MenuRemoved>
{
    private readonly IFullMenuViewMaintainer _rebuilder;
    private readonly ILogger<MenuRemovedEventHandler> _logger;

    public MenuRemovedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        ILogger<MenuRemovedEventHandler> logger)
        : base(uow, inbox)
    {
        _rebuilder = rebuilder;
        _logger = logger;
    }

    protected override async Task HandleCore(MenuRemoved notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "Handling MenuRemoved (EventId={EventId}, MenuId={MenuId}, RestaurantId={RestaurantId})",
            notification.EventId, notification.MenuId.Value, notification.RestaurantId.Value);

        await DeleteViewForRestaurantAsync(notification.RestaurantId.Value, ct);
    }

    private async Task DeleteViewForRestaurantAsync(Guid restaurantId, CancellationToken ct)
    {
        try
        {
            await _rebuilder.DeleteAsync(restaurantId, ct);
            _logger.LogInformation("Deleted FullMenuView for RestaurantId={RestaurantId} due to menu removal", restaurantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete FullMenuView for RestaurantId={RestaurantId}", restaurantId);
            // Do not throw to avoid breaking the originating command; outbox retry policy may re-run
        }
    }
}

