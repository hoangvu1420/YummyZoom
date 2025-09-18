using Dapper;
using YummyZoom.Application.Common.Caching;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IServices;

namespace YummyZoom.Infrastructure.Persistence.ReadModels.Reviews;

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
    COALESCE(AVG(r."Rating")::double precision, 0.0)                    AS avg_rating,
    COALESCE(COUNT(*)::int, 0)                                          AS total_reviews,
    COALESCE(SUM(CASE WHEN r."Rating" = 1 THEN 1 ELSE 0 END)::int,0)    AS ratings1,
    COALESCE(SUM(CASE WHEN r."Rating" = 2 THEN 1 ELSE 0 END)::int,0)    AS ratings2,
    COALESCE(SUM(CASE WHEN r."Rating" = 3 THEN 1 ELSE 0 END)::int,0)    AS ratings3,
    COALESCE(SUM(CASE WHEN r."Rating" = 4 THEN 1 ELSE 0 END)::int,0)    AS ratings4,
    COALESCE(SUM(CASE WHEN r."Rating" = 5 THEN 1 ELSE 0 END)::int,0)    AS ratings5,
    COALESCE(SUM(CASE WHEN r."Comment" IS NOT NULL AND length(btrim(r."Comment")) > 0 THEN 1 ELSE 0 END)::int,0) AS total_with_text,
    MAX(r."SubmissionTimestamp")                                        AS last_review_at
  FROM "Reviews" r
  WHERE r."RestaurantId" = @RestaurantId
    AND r."IsDeleted" = FALSE
    AND r."IsHidden" = FALSE
)
INSERT INTO "RestaurantReviewSummaries" (
    "RestaurantId",
    "AverageRating",
    "TotalReviews",
    "Ratings1","Ratings2","Ratings3","Ratings4","Ratings5",
    "TotalWithText",
    "LastReviewAtUtc",
    "UpdatedAtUtc")
SELECT @RestaurantId, s.avg_rating, s.total_reviews,
       s.ratings1, s.ratings2, s.ratings3, s.ratings4, s.ratings5,
       s.total_with_text,
       s.last_review_at,
       NOW()
FROM s
ON CONFLICT ("RestaurantId") DO UPDATE
SET "AverageRating" = EXCLUDED."AverageRating",
    "TotalReviews"  = EXCLUDED."TotalReviews",
    "Ratings1"      = EXCLUDED."Ratings1",
    "Ratings2"      = EXCLUDED."Ratings2",
    "Ratings3"      = EXCLUDED."Ratings3",
    "Ratings4"      = EXCLUDED."Ratings4",
    "Ratings5"      = EXCLUDED."Ratings5",
    "TotalWithText" = EXCLUDED."TotalWithText",
    "LastReviewAtUtc" = EXCLUDED."LastReviewAtUtc",
    "UpdatedAtUtc"  = EXCLUDED."UpdatedAtUtc";
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
