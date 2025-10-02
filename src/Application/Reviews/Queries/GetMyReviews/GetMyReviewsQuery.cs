using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Application.Reviews.Queries.Common;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Reviews.Queries.GetMyReviews;

[Authorize(Policy = YummyZoom.SharedKernel.Constants.Policies.CompletedSignup)]
public sealed record GetMyReviewsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<Result<PaginatedList<ReviewDto>>>;

public sealed class GetMyReviewsQueryHandler : IRequestHandler<GetMyReviewsQuery, Result<PaginatedList<ReviewDto>>>
{
    private readonly IDbConnectionFactory _db;
    private readonly IUser _user;

    public GetMyReviewsQueryHandler(IDbConnectionFactory db, IUser user)
    {
        _db = db;
        _user = user;
    }

    public async Task<Result<PaginatedList<ReviewDto>>> Handle(GetMyReviewsQuery request, CancellationToken cancellationToken)
    {
        if (_user.DomainUserId is null)
        {
            throw new UnauthorizedAccessException();
        }

        using var conn = _db.CreateConnection();

        const string select = """
  r."Id"                 AS ReviewId,
  r."CustomerId"         AS AuthorUserId,
  r."Rating"             AS Rating,
  null::text             AS Title,
  r."Comment"            AS Comment,
  r."SubmissionTimestamp" AS SubmittedAtUtc
""";

        const string fromWhere = "FROM \"Reviews\" r WHERE r.\"CustomerId\" = @UserId AND r.\"IsDeleted\" = FALSE";

        // DapperPagination expects split count/page SQL
        var (countSql, pageSql) = DapperPagination.BuildPagedSql(
            selectColumns: select,
            fromAndWhere: fromWhere,
            orderByClause: "r.\"SubmissionTimestamp\" DESC, r.\"Id\" DESC",
            pageNumber: request.PageNumber,
            pageSize: request.PageSize);

        var page = await conn.QueryPageAsync<ReviewDto>(
            countSql,
            pageSql,
            new { UserId = _user.DomainUserId.Value },
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return Result.Success(page);
    }
}
