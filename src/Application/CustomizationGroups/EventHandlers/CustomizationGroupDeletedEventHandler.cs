using Microsoft.Extensions.Logging;
using YummyZoom.Application.CustomizationGroups.EventHandlers.Shared;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.CustomizationGroupAggregate.Events;

namespace YummyZoom.Application.CustomizationGroups.EventHandlers;

public sealed class CustomizationGroupDeletedEventHandler : CustomizationGroupProjectorBase<CustomizationGroupDeleted>
{
    public CustomizationGroupDeletedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        ICustomizationGroupRepository groupRepository,
        ILogger<CustomizationGroupDeletedEventHandler> logger)
        : base(uow, inbox, rebuilder, groupRepository, logger)
    { }

    protected override async Task HandleCore(CustomizationGroupDeleted notification, CancellationToken ct)
    {
        var restaurantId = await ResolveRestaurantIdAsync(notification.CustomizationGroupId, ct);
        if (restaurantId is null) return;

        _logger.LogInformation("Handling CustomizationGroupDeleted (EventId={EventId}, GroupId={GroupId}, RestaurantId={RestaurantId})",
            notification.EventId, notification.CustomizationGroupId.Value, restaurantId.Value);

        await RebuildForRestaurantAsync(restaurantId.Value, ct);
    }
}

