using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.MenuEntity.Events;

namespace YummyZoom.Application.Menus.EventHandlers;

/// <summary>
/// Handles MenuDetailsUpdated domain events by rebuilding the FullMenuView for the affected restaurant.
/// Strategy: naive, correct-first full rebuild per restaurant.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class MenuDetailsUpdatedEventHandler : IdempotentNotificationHandler<MenuDetailsUpdated>
{
    private readonly IFullMenuViewMaintainer _rebuilder;
    private readonly ILogger<MenuDetailsUpdatedEventHandler> _logger;

    public MenuDetailsUpdatedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        ILogger<MenuDetailsUpdatedEventHandler> logger)
        : base(uow, inbox)
    {
        _rebuilder = rebuilder;
        _logger = logger;
    }

    protected override async Task HandleCore(MenuDetailsUpdated notification, CancellationToken ct)
    {
        _logger.LogDebug("Handling MenuDetailsUpdated (EventId={EventId}, MenuId={MenuId}, RestaurantId={RestaurantId})",
            notification.EventId, notification.MenuId.Value, notification.RestaurantId.Value);

        await RebuildForRestaurantAsync(notification.RestaurantId.Value, ct);
    }

    private async Task RebuildForRestaurantAsync(Guid restaurantId, CancellationToken ct)
    {
        try
        {
            var (menuJson, rebuiltAt) = await _rebuilder.RebuildAsync(restaurantId, ct);
            await _rebuilder.UpsertAsync(restaurantId, menuJson, rebuiltAt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rebuild FullMenuView for RestaurantId={RestaurantId}", restaurantId);
            // Do not throw to avoid breaking the originating command; outbox retry policy may re-run
        }
    }
}
