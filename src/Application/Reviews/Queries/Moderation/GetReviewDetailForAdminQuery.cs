using System.Data;
using Dapper;
using YummyZoom.Application.Common.Interfaces;

namespace YummyZoom.Application.Reviews.Queries.Moderation;

public sealed record GetReviewDetailForAdminQuery(Guid ReviewId) : IRequest<YummyZoom.SharedKernel.Result<AdminModerationReviewDetailDto>>;

public sealed class GetReviewDetailForAdminQueryHandler : IRequestHandler<GetReviewDetailForAdminQuery, YummyZoom.SharedKernel.Result<AdminModerationReviewDetailDto>>
{
    private readonly IDbConnectionFactory _db;

    public GetReviewDetailForAdminQueryHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<YummyZoom.SharedKernel.Result<AdminModerationReviewDetailDto>> Handle(GetReviewDetailForAdminQuery request, CancellationToken cancellationToken)
    {
        using var connection = _db.CreateConnection();

        const string sql = """
SELECT r."Id" AS ReviewId,
       r."RestaurantId" AS RestaurantId,
       rest."Name" AS RestaurantName,
       r."CustomerId" AS CustomerId,
       r."Rating" AS Rating,
       r."Comment" AS Comment,
       r."Reply" AS Reply,
       r."OrderId" AS OrderId,
       s."AverageRating" AS RestaurantAverageRating,
       s."TotalReviews" AS RestaurantTotalReviews,
       r."SubmissionTimestamp" AS SubmissionTimestamp,
       r."IsModerated" AS IsModerated,
       r."IsHidden" AS IsHidden,
       NULL::timestamp AS LastActionAtUtc
FROM "Reviews" r
JOIN "Restaurants" rest ON rest."Id" = r."RestaurantId"
LEFT JOIN "RestaurantReviewSummaries" s ON s."RestaurantId" = r."RestaurantId"
WHERE r."Id" = @ReviewId
LIMIT 1;
""";

        var dto = await connection.QuerySingleOrDefaultAsync<AdminModerationReviewDetailDto>(new CommandDefinition(sql, new { request.ReviewId }, cancellationToken: cancellationToken));
        if (dto is null)
        {
            return YummyZoom.SharedKernel.Result.Failure<AdminModerationReviewDetailDto>(YummyZoom.SharedKernel.Error.NotFound("Review.NotFound", "Review not found"));
        }

        return YummyZoom.SharedKernel.Result.Success(dto);
    }
}
