using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Home.Queries.ActiveDeals;

public sealed class ListActiveDealsQueryHandler
    : IRequestHandler<ListActiveDealsQuery, Result<IReadOnlyList<ActiveDealCardDto>>>
{
    private readonly IDbConnectionFactory _db;

    public ListActiveDealsQueryHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<ActiveDealCardDto>>> Handle(ListActiveDealsQuery request, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        // Pick the best coupon per restaurant using DISTINCT ON and an ordering that
        // prefers percentage discounts, then fixed amount, then free item; higher values first; sooner expiry preferred.
        const string sql = """
            SELECT DISTINCT ON (r."Id")
                   r."Id"         AS "RestaurantId",
                   r."Name"       AS "Name",
                   r."LogoUrl"    AS "LogoUrl",
                   CASE c."Value_Type"
                        WHEN 'Percentage'  THEN CONCAT(c."Value_PercentageValue", '% off')
                        WHEN 'FixedAmount' THEN CONCAT(c."Value_FixedAmount_Amount", ' off')
                        WHEN 'FreeItem'    THEN 'Free item'
                        ELSE 'Deal'
                   END           AS "BestCouponLabel"
            FROM "Restaurants" r
            JOIN "Coupons" c ON c."RestaurantId" = r."Id"
            WHERE r."IsDeleted" = FALSE
              AND r."IsVerified" = TRUE
              AND c."IsDeleted" = FALSE
              AND c."IsEnabled" = TRUE
              AND now() BETWEEN c."ValidityStartDate" AND c."ValidityEndDate"
            ORDER BY r."Id",
                     CASE c."Value_Type"
                         WHEN 'Percentage'  THEN 0
                         WHEN 'FixedAmount' THEN 1
                         WHEN 'FreeItem'    THEN 2
                         ELSE 3
                     END,
                     c."Value_PercentageValue" DESC NULLS LAST,
                     c."Value_FixedAmount_Amount" DESC NULLS LAST,
                     c."ValidityEndDate" ASC
            LIMIT @limit;
            """;

        var rows = await conn.QueryAsync<ActiveDealCardDto>(new CommandDefinition(sql, new { limit = request.Limit }, cancellationToken: ct));
        return Result.Success<IReadOnlyList<ActiveDealCardDto>>(rows.ToList());
    }
}

