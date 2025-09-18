using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Reviews.Queries.Common;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Reviews.Queries.GetRestaurantReviewSummary;

public sealed record GetRestaurantReviewSummaryQuery(Guid RestaurantId) : IRequest<Result<RestaurantReviewSummaryDto>>;

public sealed class GetRestaurantReviewSummaryQueryHandler : IRequestHandler<GetRestaurantReviewSummaryQuery, Result<RestaurantReviewSummaryDto>>
{
    private readonly IDbConnectionFactory _db;

    public GetRestaurantReviewSummaryQueryHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<Result<RestaurantReviewSummaryDto>> Handle(GetRestaurantReviewSummaryQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        const string sql = """
            SELECT 
                s."AverageRating",
                s."TotalReviews",
                s."Ratings1",
                s."Ratings2",
                s."Ratings3",
                s."Ratings4",
                s."Ratings5",
                s."TotalWithText",
                s."LastReviewAtUtc",
                s."UpdatedAtUtc"
            FROM "RestaurantReviewSummaries" s
            WHERE s."RestaurantId" = @RestaurantId
            LIMIT 1;
            """;

        var row = await conn.QuerySingleOrDefaultAsync<RestaurantReviewSummaryDto>(
            new CommandDefinition(sql, new { RestaurantId = request.RestaurantId }, cancellationToken: cancellationToken));

        if (row is null)
        {
            // Return empty summary defaults if not present
            return Result.Success(new RestaurantReviewSummaryDto(0, 0, 0, 0, 0, 0, 0, 0, null, DateTime.UtcNow));
        }

        return Result.Success(row);
    }
}

