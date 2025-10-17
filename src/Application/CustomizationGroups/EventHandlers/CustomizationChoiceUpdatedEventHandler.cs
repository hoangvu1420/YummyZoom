using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.CustomizationGroups.EventHandlers.Shared;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.CustomizationGroupAggregate.Events;

namespace YummyZoom.Application.CustomizationGroups.EventHandlers;

public sealed class CustomizationChoiceUpdatedEventHandler : CustomizationGroupProjectorBase<CustomizationChoiceUpdated>
{
    public CustomizationChoiceUpdatedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        ICustomizationGroupRepository groupRepository,
        ILogger<CustomizationChoiceUpdatedEventHandler> logger)
        : base(uow, inbox, rebuilder, groupRepository, logger)
    { }

    protected override async Task HandleCore(CustomizationChoiceUpdated notification, CancellationToken ct)
    {
        var restaurantId = await ResolveRestaurantIdAsync(notification.CustomizationGroupId, ct);
        if (restaurantId is null) return;

        _logger.LogDebug("Handling CustomizationChoiceUpdated (EventId={EventId}, GroupId={GroupId}, ChoiceId={ChoiceId}, RestaurantId={RestaurantId})",
            notification.EventId, notification.CustomizationGroupId.Value, notification.ChoiceId.Value, restaurantId.Value);

        await RebuildForRestaurantAsync(restaurantId.Value, ct);
    }
}

