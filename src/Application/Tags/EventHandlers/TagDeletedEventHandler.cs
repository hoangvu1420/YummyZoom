using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Application.Tags.EventHandlers.Shared;
using YummyZoom.Domain.TagEntity.Events;

namespace YummyZoom.Application.Tags.EventHandlers;

public sealed class TagDeletedEventHandler : TagProjectorBase<TagDeleted>
{
    public TagDeletedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        IDbConnectionFactory dbConnectionFactory,
        ILogger<TagDeletedEventHandler> logger)
        : base(uow, inbox, rebuilder, dbConnectionFactory, logger)
    { }

    protected override async Task HandleCore(TagDeleted notification, CancellationToken ct)
    {
        _logger.LogInformation("Handling TagDeleted (EventId={EventId}, TagId={TagId})", notification.EventId, notification.TagId.Value);

        // Even if tag is deleted, items may still reference its Guid in DietaryTagIds.
        // Rebuild menus for impacted restaurants so tagLegend reflects removal.
        var restaurantIds = await FindRestaurantsByTagAsync(notification.TagId, ct);
        foreach (var rId in restaurantIds)
        {
            await RebuildForRestaurantAsync(rId, ct);
        }
    }
}
