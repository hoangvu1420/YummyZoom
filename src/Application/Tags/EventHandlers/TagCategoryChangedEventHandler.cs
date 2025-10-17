using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Application.Tags.EventHandlers.Shared;
using YummyZoom.Domain.TagEntity.Events;

namespace YummyZoom.Application.Tags.EventHandlers;

public sealed class TagCategoryChangedEventHandler : TagProjectorBase<TagCategoryChanged>
{
    public TagCategoryChangedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        IDbConnectionFactory dbConnectionFactory,
        ILogger<TagCategoryChangedEventHandler> logger)
        : base(uow, inbox, rebuilder, dbConnectionFactory, logger)
    { }

    protected override async Task HandleCore(TagCategoryChanged notification, CancellationToken ct)
    {
        _logger.LogDebug("Handling TagCategoryChanged (EventId={EventId}, TagId={TagId})", notification.EventId, notification.TagId.Value);

        var restaurantIds = await FindRestaurantsByTagAsync(notification.TagId, ct);
        foreach (var rId in restaurantIds)
        {
            await RebuildForRestaurantAsync(rId, ct);
        }
    }
}
