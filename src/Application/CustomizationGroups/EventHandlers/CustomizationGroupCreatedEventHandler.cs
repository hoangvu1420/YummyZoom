using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.CustomizationGroups.EventHandlers.Shared;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.CustomizationGroupAggregate.Events;

namespace YummyZoom.Application.CustomizationGroups.EventHandlers;

/// <summary>
/// Handles CustomizationGroupCreated by rebuilding the FullMenuView for the affected restaurant.
/// Strategy: naive, correct-first full rebuild per restaurant.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class CustomizationGroupCreatedEventHandler : CustomizationGroupProjectorBase<CustomizationGroupCreated>
{
    public CustomizationGroupCreatedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        ICustomizationGroupRepository groupRepository,
        ILogger<CustomizationGroupCreatedEventHandler> logger)
        : base(uow, inbox, rebuilder, groupRepository, logger)
    { }

    protected override async Task HandleCore(CustomizationGroupCreated notification, CancellationToken ct)
    {
        _logger.LogDebug("Handling CustomizationGroupCreated (EventId={EventId}, GroupId={GroupId}, RestaurantId={RestaurantId})",
            notification.EventId, notification.CustomizationGroupId.Value, notification.RestaurantId.Value);

        await RebuildForRestaurantAsync(notification.RestaurantId.Value, ct);
    }
}

