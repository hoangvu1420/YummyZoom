using Dapper;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;


namespace YummyZoom.Application.Admin.Queries.ListRestaurantsForAdmin;

/// <summary>
/// Handler that composes the admin restaurant listing from the denormalized health summary projection.
/// </summary>
public sealed class ListRestaurantsForAdminQueryHandler : IRequestHandler<ListRestaurantsForAdminQuery, YummyZoom.SharedKernel.Result<PaginatedList<AdminRestaurantHealthSummaryDto>>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<ListRestaurantsForAdminQueryHandler> _logger;

    public ListRestaurantsForAdminQueryHandler(
        IDbConnectionFactory dbConnectionFactory,
        ILogger<ListRestaurantsForAdminQueryHandler> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    public async Task<YummyZoom.SharedKernel.Result<PaginatedList<AdminRestaurantHealthSummaryDto>>> Handle(ListRestaurantsForAdminQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        var filters = new List<string>();
        var parameters = new DynamicParameters();

        if (request.IsVerified.HasValue)
        {
            filters.Add("\"IsVerified\" = @IsVerified");
            parameters.Add("IsVerified", request.IsVerified.Value);
        }

        if (request.IsAcceptingOrders.HasValue)
        {
            filters.Add("\"IsAcceptingOrders\" = @IsAcceptingOrders");
            parameters.Add("IsAcceptingOrders", request.IsAcceptingOrders.Value);
        }

        if (request.MinAverageRating.HasValue)
        {
            filters.Add("\"AverageRating\" >= @MinAverageRating");
            parameters.Add("MinAverageRating", request.MinAverageRating.Value);
        }

        if (request.MinOrdersLast30Days.HasValue)
        {
            filters.Add("\"OrdersLast30Days\" >= @MinOrdersLast30Days");
            parameters.Add("MinOrdersLast30Days", request.MinOrdersLast30Days.Value);
        }

        if (request.MaxOutstandingBalance.HasValue)
        {
            filters.Add("\"OutstandingBalance\" <= @MaxOutstandingBalance");
            parameters.Add("MaxOutstandingBalance", request.MaxOutstandingBalance.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            filters.Add("\"RestaurantName\" ILIKE @Search");
            parameters.Add("Search", $"%{request.Search.Trim()}%");
        }

        var whereClause = filters.Count > 0
            ? $"WHERE {string.Join(" AND ", filters)}"
            : string.Empty;

        const string selectColumns = """
"RestaurantId"                  AS RestaurantId,
"RestaurantName"                AS RestaurantName,
"IsVerified"                    AS IsVerified,
"IsAcceptingOrders"             AS IsAcceptingOrders,
"OrdersLast7Days"               AS OrdersLast7Days,
"OrdersLast30Days"              AS OrdersLast30Days,
"RevenueLast30Days"             AS RevenueLast30Days,
"AverageRating"                 AS AverageRating,
"TotalReviews"                  AS TotalReviews,
"CouponRedemptionsLast30Days"   AS CouponRedemptionsLast30Days,
"OutstandingBalance"            AS OutstandingBalance,
"LastOrderAtUtc"                AS LastOrderAtUtc,
"UpdatedAtUtc"                  AS UpdatedAtUtc
""";

        var fromAndWhere = $"""
FROM "AdminRestaurantHealthSummaries"
{whereClause}
""";

        var orderByClause = request.SortBy switch
        {
            AdminRestaurantListSort.RevenueDescending => "\"RevenueLast30Days\" DESC, \"OrdersLast30Days\" DESC, \"RestaurantName\" ASC",
            AdminRestaurantListSort.OrdersDescending => "\"OrdersLast30Days\" DESC, \"RevenueLast30Days\" DESC, \"RestaurantName\" ASC",
            AdminRestaurantListSort.RatingDescending => "\"AverageRating\" DESC, \"OrdersLast30Days\" DESC, \"RestaurantName\" ASC",
            AdminRestaurantListSort.OutstandingBalanceDescending => "\"OutstandingBalance\" DESC, \"RestaurantName\" ASC",
            AdminRestaurantListSort.OutstandingBalanceAscending => "\"OutstandingBalance\" ASC, \"RestaurantName\" ASC",
            AdminRestaurantListSort.LastOrderDescending => "\"LastOrderAtUtc\" DESC NULLS LAST, \"RestaurantName\" ASC",
            AdminRestaurantListSort.LastOrderAscending => "\"LastOrderAtUtc\" ASC NULLS LAST, \"RestaurantName\" ASC",
            _ => "\"RevenueLast30Days\" DESC, \"OrdersLast30Days\" DESC, \"RestaurantName\" ASC"
        };

        var (countSql, pageSql) = DapperPagination.BuildPagedSql(
            selectColumns,
            fromAndWhere,
            orderByClause,
            request.PageNumber,
            request.PageSize);

        var page = await connection.QueryPageAsync<AdminRestaurantHealthSummaryDto>(
            countSql,
            pageSql,
            parameters,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        _logger.LogInformation(
            "Admin restaurant list retrieved: {Returned} of {Total} (page {Page}/{Size})",
            page.Items.Count,
            page.TotalCount,
            request.PageNumber,
            request.PageSize);

        return YummyZoom.SharedKernel.Result.Success(page);
    }
}
