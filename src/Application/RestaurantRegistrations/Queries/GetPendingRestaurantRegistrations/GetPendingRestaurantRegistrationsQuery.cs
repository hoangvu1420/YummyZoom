using Dapper;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.RestaurantRegistrations.Queries.Common;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;
using YummyZoom.SharedKernel.Constants;
using YummyZoom.Application.Orders.Queries.Common;

namespace YummyZoom.Application.RestaurantRegistrations.Queries.GetPendingRestaurantRegistrations;

[Authorize(Roles = Roles.Administrator)]
public sealed record GetPendingRestaurantRegistrationsQuery(int PageNumber, int PageSize)
    : IRequest<Result<PaginatedList<RegistrationSummaryDto>>>;

public sealed class GetPendingRestaurantRegistrationsQueryHandler : IRequestHandler<GetPendingRestaurantRegistrationsQuery, Result<PaginatedList<RegistrationSummaryDto>>>
{
    private readonly IDbConnectionFactory _db;

    public GetPendingRestaurantRegistrationsQueryHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<Result<PaginatedList<RegistrationSummaryDto>>> Handle(GetPendingRestaurantRegistrationsQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        const string selectColumns = """
  rr."Id"                 AS RegistrationId,
  rr."Name"               AS Name,
  rr."City"               AS City,
  rr."Status"::text       AS Status,
  rr."SubmittedAtUtc"     AS SubmittedAtUtc,
  rr."ReviewedAtUtc"      AS ReviewedAtUtc,
  rr."ReviewNote"         AS ReviewNote,
  rr."SubmitterUserId"    AS SubmitterUserId
""";

        const string fromWhere = "FROM \"RestaurantRegistrations\" rr WHERE rr.\"Status\" = 1"; // Pending = 1
        const string orderBy = "rr.\"SubmittedAtUtc\" ASC, rr.\"Id\" ASC";

        var (countSql, pageSql) = DapperPagination.BuildPagedSql(selectColumns, fromWhere, orderBy, request.PageNumber, request.PageSize);

        var page = await conn.QueryPageAsync<RegistrationSummaryDto>(
            countSql,
            pageSql,
            parameters: new { },
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return Result.Success(page);
    }
}
