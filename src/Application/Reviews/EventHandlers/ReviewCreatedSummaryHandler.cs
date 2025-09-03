using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.ReviewAggregate.Events;

namespace YummyZoom.Application.Reviews.EventHandlers;

public sealed class ReviewCreatedSummaryHandler : IdempotentNotificationHandler<ReviewCreated>
{
    private readonly IReviewSummaryMaintainer _maintainer;
    private readonly ISearchReadModelMaintainer _searchMaintainer;

    public ReviewCreatedSummaryHandler(IUnitOfWork uow, IInboxStore inbox, IReviewSummaryMaintainer maintainer, ISearchReadModelMaintainer searchMaintainer)
        : base(uow, inbox)
    {
        _maintainer = maintainer;
        _searchMaintainer = searchMaintainer;
    }

    protected override async Task HandleCore(ReviewCreated e, CancellationToken ct)
    {
        var sv = e.OccurredOnUtc.Ticks;
        var restaurantId = e.RestaurantId.Value;
        await _maintainer.RecomputeForRestaurantAsync(restaurantId, sv, ct);
        await _searchMaintainer.UpsertRestaurantByIdAsync(restaurantId, sv, ct);
    }
}
