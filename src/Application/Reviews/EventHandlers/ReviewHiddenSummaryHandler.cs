using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.ReviewAggregate.Events;

namespace YummyZoom.Application.Reviews.EventHandlers;

public sealed class ReviewHiddenSummaryHandler : IdempotentNotificationHandler<ReviewHidden>
{
    private readonly IReviewSummaryMaintainer _maintainer;
    private readonly ISearchReadModelMaintainer _searchMaintainer;
    private readonly IDbConnectionFactory _db;

    public ReviewHiddenSummaryHandler(IUnitOfWork uow, IInboxStore inbox, IReviewSummaryMaintainer maintainer, ISearchReadModelMaintainer searchMaintainer, IDbConnectionFactory db)
        : base(uow, inbox)
    {
        _maintainer = maintainer;
        _searchMaintainer = searchMaintainer;
        _db = db;
    }

    protected override async Task HandleCore(ReviewHidden e, CancellationToken ct)
    {
        var sv = e.OccurredOnUtc.Ticks;
        using var conn = _db.CreateConnection();
        const string sql = "SELECT \"RestaurantId\" FROM \"Reviews\" WHERE \"Id\" = @ReviewId LIMIT 1;";
        var restaurantId = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(sql, new { ReviewId = e.ReviewId.Value }, cancellationToken: ct));
        if (!restaurantId.HasValue)
        {
            return;
        }
        await _maintainer.RecomputeForRestaurantAsync(restaurantId.Value, sv, ct);
        await _searchMaintainer.UpsertRestaurantByIdAsync(restaurantId.Value, sv, ct);
    }
}
