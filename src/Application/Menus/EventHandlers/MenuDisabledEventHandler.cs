using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.MenuEntity.Events;

namespace YummyZoom.Application.Menus.EventHandlers;

/// <summary>
/// Handles MenuDisabled domain events by rebuilding the FullMenuView for the affected restaurant.
/// Strategy: naive, correct-first full rebuild per restaurant.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class MenuDisabledEventHandler : IdempotentNotificationHandler<MenuDisabled>
{
    private readonly IMenuReadModelRebuilder _rebuilder;
    private readonly IMenuRepository _menuRepository;
    private readonly ILogger<MenuDisabledEventHandler> _logger;

    public MenuDisabledEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IMenuReadModelRebuilder rebuilder,
        IMenuRepository menuRepository,
        ILogger<MenuDisabledEventHandler> logger)
        : base(uow, inbox)
    {
        _rebuilder = rebuilder;
        _menuRepository = menuRepository;
        _logger = logger;
    }

    protected override async Task HandleCore(MenuDisabled notification, CancellationToken ct)
    {
        _logger.LogInformation("Handling MenuDisabled (EventId={EventId}, MenuId={MenuId})",
            notification.EventId, notification.MenuId.Value);

        // Get the restaurant ID from the menu
        var menu = await _menuRepository.GetByIdAsync(notification.MenuId, ct);
        if (menu is null)
        {
            _logger.LogWarning("Menu with ID {MenuId} not found when handling MenuDisabled event", notification.MenuId.Value);
            return;
        }

        await DeleteViewForRestaurantAsync(menu.RestaurantId.Value, ct);
    }

    private async Task DeleteViewForRestaurantAsync(Guid restaurantId, CancellationToken ct)
    {
        try
        {
            await _rebuilder.DeleteAsync(restaurantId, ct);
            _logger.LogInformation("Deleted FullMenuView for RestaurantId={RestaurantId} due to menu being disabled", restaurantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete FullMenuView for RestaurantId={RestaurantId}", restaurantId);
            // Do not throw to avoid breaking the originating command; outbox retry policy may re-run
        }
    }
}
