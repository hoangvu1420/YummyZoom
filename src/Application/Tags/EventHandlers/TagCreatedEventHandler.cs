using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Application.Tags.EventHandlers.Shared;
using YummyZoom.Domain.TagEntity.Events;

namespace YummyZoom.Application.Tags.EventHandlers;

public sealed class TagCreatedEventHandler : TagProjectorBase<TagCreated>
{
    public TagCreatedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        IDbConnectionFactory dbConnectionFactory,
        ILogger<TagCreatedEventHandler> logger)
        : base(uow, inbox, rebuilder, dbConnectionFactory, logger)
    { }

    protected override async Task HandleCore(TagCreated notification, CancellationToken ct)
    {
        _logger.LogInformation("Handling TagCreated (EventId={EventId}, TagId={TagId})", notification.EventId, notification.TagId.Value);

        // Usually no items reference a brand new tag yet; safe no-op.
        var restaurantIds = await FindRestaurantsByTagAsync(notification.TagId, ct);
        foreach (var rId in restaurantIds)
        {
            await RebuildForRestaurantAsync(rId, ct);
        }
    }
}
