using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.RestaurantRegistrations.Queries.Common;
using YummyZoom.Domain.RestaurantRegistrationAggregate.Errors;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.RestaurantRegistrations.Queries.GetPendingRestaurantRegistrationDetailForAdmin;

[Authorize(Roles = Roles.Administrator)]
public sealed record GetPendingRestaurantRegistrationDetailForAdminQuery(Guid RegistrationId)
    : IRequest<Result<RegistrationDetailForAdminDto>>;

public sealed class GetPendingRestaurantRegistrationDetailForAdminQueryHandler
    : IRequestHandler<GetPendingRestaurantRegistrationDetailForAdminQuery, Result<RegistrationDetailForAdminDto>>
{
    private readonly IDbConnectionFactory _db;

    public GetPendingRestaurantRegistrationDetailForAdminQueryHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<Result<RegistrationDetailForAdminDto>> Handle(GetPendingRestaurantRegistrationDetailForAdminQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        const string statusSql = "SELECT rr.\"Status\" FROM \"RestaurantRegistrations\" rr WHERE rr.\"Id\" = @RegistrationId;";
        var status = await conn.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(statusSql, new { request.RegistrationId }, cancellationToken: cancellationToken));
        if (status is null)
        {
            return Result.Failure<RegistrationDetailForAdminDto>(RestaurantRegistrationErrors.NotFound(request.RegistrationId));
        }

        if (status != 1)
        {
            return Result.Failure<RegistrationDetailForAdminDto>(RestaurantRegistrationErrors.NotPending);
        }

        const string detailSql = """
SELECT rr."Id" AS RegistrationId,
       rr."Name" AS Name,
       rr."Description" AS Description,
       rr."CuisineType" AS CuisineType,
       rr."Street" AS Street,
       rr."City" AS City,
       rr."State" AS State,
       rr."ZipCode" AS ZipCode,
       rr."Country" AS Country,
       rr."PhoneNumber" AS PhoneNumber,
       rr."Email" AS Email,
       rr."BusinessHours" AS BusinessHours,
       rr."LogoUrl" AS LogoUrl,
       rr."Latitude" AS Latitude,
       rr."Longitude" AS Longitude,
       CASE rr."Status"
           WHEN 1 THEN 'Pending'
           WHEN 2 THEN 'Approved'
           WHEN 3 THEN 'Rejected'
           ELSE 'Unknown'
       END AS Status,
       rr."SubmittedAtUtc" AS SubmittedAtUtc,
       rr."ReviewedAtUtc" AS ReviewedAtUtc,
       rr."ReviewNote" AS ReviewNote,
       rr."SubmitterUserId" AS SubmitterUserId,
       du."Name" AS SubmitterName,
       rr."ReviewedByUserId" AS ReviewedByUserId
FROM "RestaurantRegistrations" rr
JOIN "DomainUsers" du ON du."Id" = rr."SubmitterUserId"
WHERE rr."Id" = @RegistrationId
LIMIT 1;
""";

        var dto = await conn.QuerySingleAsync<RegistrationDetailForAdminDto>(
            new CommandDefinition(detailSql, new { request.RegistrationId }, cancellationToken: cancellationToken));

        return Result.Success(dto);
    }
}
