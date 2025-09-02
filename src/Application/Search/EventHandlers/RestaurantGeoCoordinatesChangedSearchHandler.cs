using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.RestaurantAggregate.Events;

namespace YummyZoom.Application.Search.EventHandlers;

public sealed class RestaurantGeoCoordinatesChangedSearchHandler : IdempotentNotificationHandler<RestaurantGeoCoordinatesChanged>
{
    private readonly ISearchReadModelMaintainer _maintainer;

    public RestaurantGeoCoordinatesChangedSearchHandler(IUnitOfWork uow, IInboxStore inbox, ISearchReadModelMaintainer maintainer)
        : base(uow, inbox)
    {
        _maintainer = maintainer;
    }

    protected override async Task HandleCore(RestaurantGeoCoordinatesChanged e, CancellationToken ct)
    {
        await _maintainer.UpsertRestaurantByIdAsync(e.RestaurantId.Value, e.OccurredOnUtc.Ticks, ct);
    }
}
