using Microsoft.Extensions.Logging;
using YummyZoom.Application.CustomizationGroups.EventHandlers.Shared;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.CustomizationGroupAggregate.Events;

namespace YummyZoom.Application.CustomizationGroups.EventHandlers;

public sealed class CustomizationChoicesReorderedEventHandler : CustomizationGroupProjectorBase<CustomizationChoicesReordered>
{
    public CustomizationChoicesReorderedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        ICustomizationGroupRepository groupRepository,
        ILogger<CustomizationChoicesReorderedEventHandler> logger)
        : base(uow, inbox, rebuilder, groupRepository, logger)
    { }

    protected override async Task HandleCore(CustomizationChoicesReordered notification, CancellationToken ct)
    {
        var restaurantId = await ResolveRestaurantIdAsync(notification.CustomizationGroupId, ct);
        if (restaurantId is null) return;

        _logger.LogInformation("Handling CustomizationChoicesReordered (EventId={EventId}, GroupId={GroupId}, RestaurantId={RestaurantId})",
            notification.EventId, notification.CustomizationGroupId.Value, restaurantId.Value);

        await RebuildForRestaurantAsync(restaurantId.Value, ct);
    }
}

