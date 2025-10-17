using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.MenuEntity.Events;

namespace YummyZoom.Application.MenuCategories.EventHandlers;

/// <summary>
/// Handles MenuCategoryAdded domain events by rebuilding the FullMenuView for the affected restaurant.
/// Strategy: naive, correct-first full rebuild per restaurant.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class MenuCategoryAddedEventHandler : IdempotentNotificationHandler<MenuCategoryAdded>
{
    private readonly IFullMenuViewMaintainer _rebuilder;
    private readonly IMenuRepository _menuRepository;
    private readonly ILogger<MenuCategoryAddedEventHandler> _logger;

    public MenuCategoryAddedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        IMenuRepository menuRepository,
        ILogger<MenuCategoryAddedEventHandler> logger)
        : base(uow, inbox)
    {
        _rebuilder = rebuilder;
        _menuRepository = menuRepository;
        _logger = logger;
    }

    protected override async Task HandleCore(MenuCategoryAdded notification, CancellationToken ct)
    {
        _logger.LogDebug("Handling MenuCategoryAdded (EventId={EventId}, CategoryId={CategoryId}, MenuId={MenuId})",
            notification.EventId, notification.CategoryId.Value, notification.MenuId.Value);

        // Get the restaurant ID from the menu
        var menu = await _menuRepository.GetByIdAsync(notification.MenuId, ct);
        if (menu is null)
        {
            _logger.LogWarning("Menu with ID {MenuId} not found when handling MenuCategoryAdded event", notification.MenuId.Value);
            return;
        }

        await RebuildForRestaurantAsync(menu.RestaurantId.Value, ct);
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
