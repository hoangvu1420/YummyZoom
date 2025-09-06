using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Caching;

namespace YummyZoom.Infrastructure.Data.ReadModels.Reviews;

public sealed class ReviewSummaryMaintainer : IReviewSummaryMaintainer
{
    private readonly IDbConnectionFactory _db;
    private readonly ICacheInvalidationPublisher _invalidation;

    public ReviewSummaryMaintainer(IDbConnectionFactory db, ICacheInvalidationPublisher invalidation)
    {
        _db = db;
        _invalidation = invalidation;
    }

    public async Task RecomputeForRestaurantAsync(Guid restaurantId, long sourceVersion, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        const string sql = """
WITH s AS (
  SELECT
    COALESCE(AVG(r."Rating")::double precision, 0.0)    AS avg_rating,
    COALESCE(COUNT(*)::int, 0)                          AS total_reviews
  FROM "Reviews" r
  WHERE r."RestaurantId" = @RestaurantId
    AND r."IsDeleted" = FALSE
    AND r."IsHidden" = FALSE
)
INSERT INTO "RestaurantReviewSummaries" ("RestaurantId", "AverageRating", "TotalReviews")
SELECT @RestaurantId, s.avg_rating, s.total_reviews FROM s
ON CONFLICT ("RestaurantId") DO UPDATE
SET "AverageRating" = EXCLUDED."AverageRating",
    "TotalReviews" = EXCLUDED."TotalReviews";
""";

        await conn.ExecuteAsync(new CommandDefinition(sql, new { RestaurantId = restaurantId }, cancellationToken: ct));

        // Publish cache invalidation for review summary caches
        await _invalidation.PublishAsync(new CacheInvalidationMessage
        {
            Tags = new[] { $"restaurant:{restaurantId:N}:reviews" },
            Reason = "ReviewSummaryUpsert",
            SourceEvent = "ReviewSummaryMaintainer"
        }, ct);
    }

    public async Task RecomputeForReviewAsync(Guid reviewId, long sourceVersion, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        const string lookupSql = """
SELECT "RestaurantId"
FROM "Reviews"
WHERE "Id" = @ReviewId
LIMIT 1;
""";

        var restaurantId = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(lookupSql, new { ReviewId = reviewId }, cancellationToken: ct));
        if (!restaurantId.HasValue)
        {
            return; // review not found; nothing to recompute
        }

        await RecomputeForRestaurantAsync(restaurantId.Value, sourceVersion, ct);
    }
}
