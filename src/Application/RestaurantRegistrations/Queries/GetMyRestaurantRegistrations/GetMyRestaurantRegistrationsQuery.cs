using Dapper;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.RestaurantRegistrations.Queries.Common;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.RestaurantRegistrations.Queries.GetMyRestaurantRegistrations;

[Authorize(Policy = Policies.CompletedSignup)]
public sealed record GetMyRestaurantRegistrationsQuery() : IRequest<Result<IReadOnlyList<RegistrationSummaryDto>>>;

public sealed class GetMyRestaurantRegistrationsQueryHandler : IRequestHandler<GetMyRestaurantRegistrationsQuery, Result<IReadOnlyList<RegistrationSummaryDto>>>
{
    private readonly IDbConnectionFactory _db;
    private readonly IUser _user;

    public GetMyRestaurantRegistrationsQueryHandler(IDbConnectionFactory db, IUser user)
    {
        _db = db;
        _user = user;
    }

    public async Task<Result<IReadOnlyList<RegistrationSummaryDto>>> Handle(GetMyRestaurantRegistrationsQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        UserId userId;
        if (_user.DomainUserId is { } duid)
        {
            userId = duid;
        }
        else if (_user.Id is string sid && Guid.TryParse(sid, out var gid))
        {
            userId = UserId.Create(gid);
        }
        else
        {
            userId = UserId.Create(Guid.Empty);
        }

        const string sql = """
SELECT
  rr."Id"                 AS RegistrationId,
  rr."Name"               AS Name,
  rr."City"               AS City,
  rr."Status"::text       AS Status,
  rr."SubmittedAtUtc"     AS SubmittedAtUtc,
  rr."ReviewedAtUtc"      AS ReviewedAtUtc,
  rr."ReviewNote"         AS ReviewNote,
  rr."SubmitterUserId"    AS SubmitterUserId
FROM "RestaurantRegistrations" rr
WHERE rr."SubmitterUserId" = @UserId
ORDER BY rr."SubmittedAtUtc" DESC
""";

        var rows = await conn.QueryAsync<RegistrationSummaryDto>(
            new CommandDefinition(sql, new { UserId = userId.Value }, cancellationToken: cancellationToken));

        return Result.Success((IReadOnlyList<RegistrationSummaryDto>)rows.AsList());
    }
}
