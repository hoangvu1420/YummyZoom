using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Coupons.Queries.ListCouponsByRestaurant;

public sealed class ListCouponsByRestaurantQueryHandler
    : IRequestHandler<ListCouponsByRestaurantQuery, Result<PaginatedList<CouponSummaryDto>>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public ListCouponsByRestaurantQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<PaginatedList<CouponSummaryDto>>> Handle(
        ListCouponsByRestaurantQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var where = new List<string>
        {
            "c.\"RestaurantId\" = @RestaurantId",
            "c.\"IsDeleted\" = FALSE"
        };

        var parameters = new DynamicParameters();
        parameters.Add("RestaurantId", request.RestaurantId);

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            where.Add("(c.\"Code\" ILIKE '%' || @Q || '%' OR c.\"Description\" ILIKE '%' || @Q || '%')");
            parameters.Add("Q", request.Q);
        }

        if (request.IsEnabled.HasValue)
        {
            where.Add("c.\"IsEnabled\" = @IsEnabled");
            parameters.Add("IsEnabled", request.IsEnabled.Value);
        }

        if (request.ActiveFrom.HasValue)
        {
            where.Add("c.\"ValidityEndDate\" >= @ActiveFrom");
            parameters.Add("ActiveFrom", request.ActiveFrom.Value);
        }

        if (request.ActiveTo.HasValue)
        {
            where.Add("c.\"ValidityStartDate\" <= @ActiveTo");
            parameters.Add("ActiveTo", request.ActiveTo.Value);
        }

        const string selectColumns = """
            c."Id"                      AS "CouponId",
            c."Code"                    AS "Code",
            c."Description"             AS "Description",
            c."Value_Type"              AS "ValueType",
            c."Value_PercentageValue"   AS "Percentage",
            c."Value_FixedAmount_Amount" AS "FixedAmount",
            c."Value_FixedAmount_Currency" AS "FixedCurrency",
            c."Value_FreeItemValue"     AS "FreeItemId",
            c."AppliesTo_Scope"         AS "Scope",
            c."ValidityStartDate"       AS "ValidityStartDate",
            c."ValidityEndDate"         AS "ValidityEndDate",
            c."MinOrderAmount_Amount"   AS "MinOrderAmount",
            c."MinOrderAmount_Currency" AS "MinOrderCurrency",
            c."TotalUsageLimit"         AS "TotalUsageLimit",
            c."CurrentTotalUsageCount"  AS "CurrentTotalUsageCount",
            c."UsageLimitPerUser"       AS "UsageLimitPerUser",
            c."IsEnabled"               AS "IsEnabled",
            c."Created"                 AS "Created",
            c."LastModified"            AS "LastModified"
        """;

        var fromWhere = $"FROM \"Coupons\" c WHERE {string.Join(" AND ", where)}";
        const string orderBy = "c.\"Created\" DESC, c.\"Id\" DESC";

        var (countSql, pageSql) = DapperPagination.BuildPagedSql(
            selectColumns,
            fromWhere,
            orderBy,
            request.PageNumber,
            request.PageSize);

        var page = await connection.QueryPageAsync<CouponRow>(
            countSql,
            pageSql,
            parameters,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var mapped = page.Items
            .Select(row => new CouponSummaryDto(
                row.CouponId,
                row.Code,
                row.Description,
                Enum.Parse<CouponType>(row.ValueType, ignoreCase: true),
                row.Percentage,
                row.FixedAmount,
                row.FixedCurrency,
                row.FreeItemId,
                Enum.Parse<CouponScope>(row.Scope, ignoreCase: true),
                row.ValidityStartDate,
                row.ValidityEndDate,
                row.MinOrderAmount,
                row.MinOrderCurrency,
                row.TotalUsageLimit,
                row.CurrentTotalUsageCount,
                row.UsageLimitPerUser,
                row.IsEnabled,
                row.Created,
                row.LastModified))
            .ToList();

        var resultPage = new PaginatedList<CouponSummaryDto>(
            mapped,
            page.TotalCount,
            page.PageNumber,
            request.PageSize);

        return Result.Success(resultPage);
    }

    private sealed class CouponRow
    {
        public Guid CouponId { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string ValueType { get; init; } = string.Empty;
        public decimal? Percentage { get; init; }
        public decimal? FixedAmount { get; init; }
        public string? FixedCurrency { get; init; }
        public Guid? FreeItemId { get; init; }
        public string Scope { get; init; } = string.Empty;
        public DateTime ValidityStartDate { get; init; }
        public DateTime ValidityEndDate { get; init; }
        public decimal? MinOrderAmount { get; init; }
        public string? MinOrderCurrency { get; init; }
        public int? TotalUsageLimit { get; init; }
        public int CurrentTotalUsageCount { get; init; }
        public int? UsageLimitPerUser { get; init; }
        public bool IsEnabled { get; init; }
        public DateTimeOffset Created { get; init; }
        public DateTimeOffset LastModified { get; init; }
    }
}
