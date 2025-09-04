using Microsoft.Extensions.Logging;
using YummyZoom.Application.CustomizationGroups.EventHandlers.Shared;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.CustomizationGroupAggregate.Events;

namespace YummyZoom.Application.CustomizationGroups.EventHandlers;

public sealed class CustomizationChoiceAddedEventHandler : CustomizationGroupProjectorBase<CustomizationChoiceAdded>
{
    public CustomizationChoiceAddedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IMenuReadModelRebuilder rebuilder,
        ICustomizationGroupRepository groupRepository,
        ILogger<CustomizationChoiceAddedEventHandler> logger)
        : base(uow, inbox, rebuilder, groupRepository, logger)
    { }

    protected override async Task HandleCore(CustomizationChoiceAdded notification, CancellationToken ct)
    {
        var restaurantId = await ResolveRestaurantIdAsync(notification.CustomizationGroupId, ct);
        if (restaurantId is null) return;

        _logger.LogInformation("Handling CustomizationChoiceAdded (EventId={EventId}, GroupId={GroupId}, ChoiceId={ChoiceId}, RestaurantId={RestaurantId})",
            notification.EventId, notification.CustomizationGroupId.Value, notification.ChoiceId.Value, restaurantId.Value);

        await RebuildForRestaurantAsync(restaurantId.Value, ct);
    }
}

