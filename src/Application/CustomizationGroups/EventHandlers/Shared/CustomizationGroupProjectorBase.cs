using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;

namespace YummyZoom.Application.CustomizationGroups.EventHandlers.Shared;

/// <summary>
/// Shared base for CustomizationGroup event projectors that rebuild the FullMenuView.
/// - Ensures idempotency via IdempotentNotificationHandler
/// - Provides helpers to rebuild the restaurant menu JSON and resolve RestaurantId from CustomizationGroupId
/// </summary>
public abstract class CustomizationGroupProjectorBase<TEvent> : IdempotentNotificationHandler<TEvent>
    where TEvent : IDomainEvent, IHasEventId
{
    protected readonly IFullMenuViewMaintainer _rebuilder;
    protected readonly ICustomizationGroupRepository _groupRepository;
    protected readonly ILogger _logger;

    protected CustomizationGroupProjectorBase(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        ICustomizationGroupRepository groupRepository,
        ILogger logger) : base(uow, inbox)
    {
        _rebuilder = rebuilder;
        _groupRepository = groupRepository;
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

    protected async Task<Guid?> ResolveRestaurantIdAsync(CustomizationGroupId groupId, CancellationToken ct)
    {
        var restaurantId = await _groupRepository.GetRestaurantIdByIdIncludingDeletedAsync(groupId, ct);
        if (restaurantId is null)
        {
            _logger.LogWarning("Could not resolve RestaurantId for CustomizationGroupId={CustomizationGroupId}; projector will no-op.", groupId.Value);
            return null;
        }
        return restaurantId.Value;
    }
}

