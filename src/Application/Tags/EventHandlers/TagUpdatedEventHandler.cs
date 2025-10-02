using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Application.Tags.EventHandlers.Shared;
using YummyZoom.Domain.TagEntity.Events;

namespace YummyZoom.Application.Tags.EventHandlers;

public sealed class TagUpdatedEventHandler : TagProjectorBase<TagUpdated>
{
    public TagUpdatedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IFullMenuViewMaintainer rebuilder,
        IDbConnectionFactory dbConnectionFactory,
        ILogger<TagUpdatedEventHandler> logger)
        : base(uow, inbox, rebuilder, dbConnectionFactory, logger)
    { }

    protected override async Task HandleCore(TagUpdated notification, CancellationToken ct)
    {
        _logger.LogInformation("Handling TagUpdated (EventId={EventId}, TagId={TagId})", notification.EventId, notification.TagId.Value);

        var restaurantIds = await FindRestaurantsByTagAsync(notification.TagId, ct);
        foreach (var rId in restaurantIds)
        {
            await RebuildForRestaurantAsync(rId, ct);
        }
    }
}
