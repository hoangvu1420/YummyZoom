using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Admin.Queries.GetPlatformMetricsSummary;
using YummyZoom.Application.Admin.Queries.GetPlatformTrends;
using YummyZoom.Application.Admin.Queries.ListRestaurantsForAdmin;
using YummyZoom.Application.Common.Models;
using YummyZoom.SharedKernel.Constants;
using YummyZoom.Web.Infrastructure;

namespace YummyZoom.Web.Endpoints;

/// <summary>
/// Minimal API surface supplying admin dashboard data (metrics, trends, restaurant health).
/// </summary>
public sealed class AdminDashboards : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/admin/dashboard")
            .WithGroupName(nameof(AdminDashboards))
            .WithTags(nameof(AdminDashboards))
            .RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Administrator });

        MapSummaryEndpoint(group);
        MapTrendsEndpoint(group);
        MapRestaurantsEndpoint(group);
    }

    private static void MapSummaryEndpoint(RouteGroupBuilder group)
    {
        group.MapGet(
            "/summary",
            async (ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetPlatformMetricsSummaryQuery(), cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
            })
            .WithName("GetAdminDashboardSummary")
            .WithSummary("Get admin platform metrics summary")
            .WithDescription("Returns the latest platform KPIs (orders, GMV, active entities, review totals).")
            .WithStandardResults<AdminPlatformMetricsSummaryDto>();
    }

    private static void MapTrendsEndpoint(RouteGroupBuilder group)
    {
        group.MapGet(
            "/trends",
            async ([FromQuery] DateOnly? startDate,
                   [FromQuery] DateOnly? endDate,
                   ISender sender,
                   CancellationToken cancellationToken) =>
            {
                var query = new GetPlatformTrendsQuery(startDate, endDate);
                var result = await sender.Send(query, cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
            })
            .WithName("GetAdminDashboardTrends")
            .WithSummary("Get admin platform performance series")
            .WithDescription("Returns daily buckets for orders, GMV, refunds, and new accounts within the requested window.")
            .WithStandardResults<IReadOnlyList<AdminDailyPerformancePointDto>>();
    }

    private static void MapRestaurantsEndpoint(RouteGroupBuilder group)
    {
        group.MapGet(
            "/restaurants",
            async ([FromQuery] int? pageNumber,
                   [FromQuery] int? pageSize,
                   [FromQuery] bool? isVerified,
                   [FromQuery] bool? isAcceptingOrders,
                   [FromQuery] double? minAverageRating,
                   [FromQuery] int? minOrdersLast30Days,
                   [FromQuery] decimal? maxOutstandingBalance,
                   [FromQuery] string? search,
                   [FromQuery] AdminRestaurantListSort? sortBy,
                   ISender sender,
                   CancellationToken cancellationToken) =>
            {
                var query = new ListRestaurantsForAdminQuery(
                    PageNumber: pageNumber ?? 1,
                    PageSize: pageSize ?? 25,
                    IsVerified: isVerified,
                    IsAcceptingOrders: isAcceptingOrders,
                    MinAverageRating: minAverageRating,
                    MinOrdersLast30Days: minOrdersLast30Days,
                    MaxOutstandingBalance: maxOutstandingBalance,
                    Search: search,
                    SortBy: sortBy ?? AdminRestaurantListSort.RevenueDescending);

                var result = await sender.Send(query, cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
            })
            .WithName("ListAdminRestaurants")
            .WithSummary("List restaurant health summaries for admins")
            .WithDescription("Provides paginated restaurant metrics with optional filters and sorting for admin dashboards.")
            .WithStandardResults<PaginatedList<AdminRestaurantHealthSummaryDto>>();
    }
}
