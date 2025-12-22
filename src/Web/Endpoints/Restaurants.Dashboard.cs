using YummyZoom.Application.Restaurants.Queries.Management.GetRestaurantDashboardSummary;

namespace YummyZoom.Web.Endpoints;

public partial class Restaurants
{
    private static void MapDashboard(IEndpointRouteBuilder group)
    {
        // GET /api/v1/restaurants/{restaurantId}/dashboard/summary
        group.MapGet("/{restaurantId:guid}/dashboard/summary", async (
            Guid restaurantId,
            int? topItemsLimit,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new GetRestaurantDashboardSummaryQuery(restaurantId, topItemsLimit);
            var result = await sender.Send(query, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetRestaurantDashboardSummary")
        .WithSummary("Get restaurant dashboard summary")
        .WithDescription("Returns aggregated KPIs for a single restaurant, including orders, revenue, reviews, and top-selling items. Requires restaurant staff authorization.")
        .WithStandardResults<RestaurantDashboardSummaryDto>();
    }
}
