using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Application.Reviews.Queries.Common;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Reviews.Queries.GetRestaurantReviews;

public sealed record GetRestaurantReviewsQuery(Guid RestaurantId, int PageNumber, int PageSize)
    : IRequest<Result<PaginatedList<ReviewDto>>>;

public sealed class GetRestaurantReviewsQueryHandler : IRequestHandler<GetRestaurantReviewsQuery, Result<PaginatedList<ReviewDto>>>
{
    private readonly IDbConnectionFactory _db;

    public GetRestaurantReviewsQueryHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<Result<PaginatedList<ReviewDto>>> Handle(GetRestaurantReviewsQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        const string selectColumns = """
            r."Id"                 AS ReviewId,
            r."CustomerId"         AS AuthorUserId,
            r."Rating"             AS Rating,
            null::text             AS Title,
            r."Comment"            AS Comment,
            r."SubmissionTimestamp" AS SubmittedAtUtc
        """;

        const string fromWhere = "FROM \"Reviews\" r WHERE r.\"RestaurantId\" = @RestaurantId AND r.\"IsDeleted\" = FALSE AND r.\"IsHidden\" = FALSE";
        const string orderBy = "r.\"SubmissionTimestamp\" DESC, r.\"Id\" DESC";

        var (countSql, pageSql) = DapperPagination.BuildPagedSql(selectColumns, fromWhere, orderBy, request.PageNumber, request.PageSize);

        var page = await conn.QueryPageAsync<ReviewDto>(
            countSql,
            pageSql,
            new { RestaurantId = request.RestaurantId },
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return Result.Success(page);
    }
}
