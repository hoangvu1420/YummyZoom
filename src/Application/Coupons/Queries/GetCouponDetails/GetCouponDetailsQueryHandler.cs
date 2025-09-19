using System.Text.Json;
using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Coupons.Queries.GetCouponDetails;

public sealed class GetCouponDetailsQueryHandler
    : IRequestHandler<GetCouponDetailsQuery, Result<CouponDetailsDto>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetCouponDetailsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<CouponDetailsDto>> Handle(GetCouponDetailsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                c."Id"                      AS "CouponId",
                c."Code"                    AS "Code",
                c."Description"             AS "Description",
                c."Value_Type"              AS "ValueType",
                c."Value_PercentageValue"   AS "Percentage",
                c."Value_FixedAmount_Amount" AS "FixedAmount",
                c."Value_FixedAmount_Currency" AS "FixedCurrency",
                c."Value_FreeItemValue"     AS "FreeItemId",
                c."AppliesTo_Scope"         AS "Scope",
                c."AppliesTo_ItemIds"       AS "ItemIdsJson",
                c."AppliesTo_CategoryIds"   AS "CategoryIdsJson",
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
            FROM "Coupons" c
            WHERE c."Id" = @CouponId
              AND c."RestaurantId" = @RestaurantId
              AND c."IsDeleted" = FALSE
            LIMIT 1
        """;

        var row = await connection.QuerySingleOrDefaultAsync<CouponRow>(
            new CommandDefinition(sql, new { request.CouponId, request.RestaurantId }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return Result.Failure<CouponDetailsDto>(GetCouponDetailsErrors.NotFound(request.CouponId));
        }

        var valueType = Enum.Parse<CouponType>(row.ValueType, ignoreCase: true);
        var scope = Enum.Parse<CouponScope>(row.Scope, ignoreCase: true);

        var itemIds = ParseGuidArray(row.ItemIdsJson);
        var categoryIds = ParseGuidArray(row.CategoryIdsJson);

        var dto = new CouponDetailsDto(
            CouponId: row.CouponId,
            Code: row.Code,
            Description: row.Description,
            ValueType: valueType,
            Percentage: row.Percentage,
            FixedAmount: row.FixedAmount,
            FixedCurrency: row.FixedCurrency,
            FreeItemId: row.FreeItemId,
            Scope: scope,
            ItemIds: itemIds,
            CategoryIds: categoryIds,
            ValidityStartDate: row.ValidityStartDate,
            ValidityEndDate: row.ValidityEndDate,
            MinOrderAmount: row.MinOrderAmount,
            MinOrderCurrency: row.MinOrderCurrency,
            TotalUsageLimit: row.TotalUsageLimit,
            CurrentTotalUsageCount: row.CurrentTotalUsageCount,
            UsageLimitPerUser: row.UsageLimitPerUser,
            IsEnabled: row.IsEnabled,
            Created: row.Created,
            LastModified: row.LastModified);

        return Result.Success(dto);
    }

    private static IReadOnlyList<Guid> ParseGuidArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<Guid>();
        }

        try
        {
            var values = JsonSerializer.Deserialize<List<Guid>>(json, JsonOptions);
            return values?.Count > 0 ? values : Array.Empty<Guid>();
        }
        catch (JsonException)
        {
            return Array.Empty<Guid>();
        }
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
        public string? ItemIdsJson { get; init; }
        public string? CategoryIdsJson { get; init; }
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
