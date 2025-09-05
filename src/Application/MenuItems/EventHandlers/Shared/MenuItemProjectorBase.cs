using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;

namespace YummyZoom.Application.MenuItems.EventHandlers.Shared;

/// <summary>
/// Shared base for MenuItem event projectors that rebuild the FullMenuView.
/// - Ensures idempotency via IdempotentNotificationHandler
/// - Provides helpers to rebuild the restaurant menu JSON and resolve RestaurantId from MenuItemId
/// </summary>
public abstract class MenuItemProjectorBase<TEvent> : IdempotentNotificationHandler<TEvent>
    where TEvent : IDomainEvent, IHasEventId
{
    protected readonly IFullMenuViewMaintainer _rebuilder;
    protected readonly IMenuItemRepository _menuItemRepository;
    protected readonly ILogger _logger;

    protected MenuItemProjectorBase(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        IMenuItemRepository menuItemRepository,
        ILogger logger) : base(uow, inbox)
    {
        _rebuilder = rebuilder;
        _menuItemRepository = menuItemRepository;
        _logger = logger;
    }

    protected async Task RebuildForRestaurantAsync(Guid restaurantId, CancellationToken ct)
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

    protected async Task<Guid?> ResolveRestaurantIdAsync(MenuItemId menuItemId, CancellationToken ct)
    {
        var restaurantId = await _menuItemRepository.GetRestaurantIdByIdIncludingDeletedAsync(menuItemId, ct);
        if (restaurantId is null)
        {
            _logger.LogWarning("Could not resolve RestaurantId for MenuItemId={MenuItemId}; projector will no-op.", menuItemId.Value);
            return null;
        }
        return restaurantId.Value;
    }
}

