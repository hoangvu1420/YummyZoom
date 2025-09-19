using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Coupons.Queries.GetCouponStats;

public sealed class GetCouponStatsQueryHandler
    : IRequestHandler<GetCouponStatsQuery, Result<CouponStatsDto>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetCouponStatsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<CouponStatsDto>> Handle(GetCouponStatsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                COUNT(o."Id") AS "TotalUsage",
                COUNT(DISTINCT o."CustomerId") AS "UniqueUsers",
                MAX(o."PlacementTimestamp") AS "LastUsedAt"
            FROM "Coupons" c
            LEFT JOIN "Orders" o ON o."AppliedCouponId" = c."Id" AND o."RestaurantId" = c."RestaurantId"
            WHERE c."Id" = @CouponId
              AND c."RestaurantId" = @RestaurantId
              AND c."IsDeleted" = FALSE
            GROUP BY c."Id"
        """;

        var row = await connection.QueryFirstOrDefaultAsync<CouponStatsRow>(
            new CommandDefinition(sql, new { request.CouponId, request.RestaurantId }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return Result.Failure<CouponStatsDto>(GetCouponStatsErrors.NotFound(request.CouponId));
        }

        DateTime? lastUsedUtc = null;
        if (row.LastUsedAt.HasValue)
        {
            lastUsedUtc = DateTime.SpecifyKind(row.LastUsedAt.Value, DateTimeKind.Utc);
        }

        var dto = new CouponStatsDto(
            TotalUsage: row.TotalUsage,
            UniqueUsers: row.UniqueUsers,
            LastUsedAtUtc: lastUsedUtc);

        return Result.Success(dto);
    }

    private sealed class CouponStatsRow
    {
        public int TotalUsage { get; init; }
        public int UniqueUsers { get; init; }
        public DateTime? LastUsedAt { get; init; }
    }
}
