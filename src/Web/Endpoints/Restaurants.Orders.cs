using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Application.Orders.Queries.GetRestaurantActiveOrders;
using YummyZoom.Application.Orders.Queries.GetRestaurantNewOrders;

namespace YummyZoom.Web.Endpoints;

public partial class Restaurants
{
    private static void MapOrders(IEndpointRouteBuilder group)
    {
        // GET /api/v1/restaurants/{restaurantId}/orders/new
        group.MapGet("/{restaurantId:guid}/orders/new", async (Guid restaurantId, int? pageNumber, int? pageSize, ISender sender) =>
        {
            // Apply defaults after binding to avoid Minimal API early 400s for missing value-type properties
            var page = pageNumber ?? 1;
            var size = pageSize ?? 10;

            var query = new GetRestaurantNewOrdersQuery(restaurantId, page, size);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetRestaurantNewOrders")
        .WithStandardResults<PaginatedList<OrderSummaryDto>>();

        // GET /api/v1/restaurants/{restaurantId}/orders/active
        group.MapGet("/{restaurantId:guid}/orders/active", async (Guid restaurantId, int? pageNumber, int? pageSize, ISender sender) =>
        {
            // Apply defaults after binding to avoid Minimal API early 400s for missing value-type properties
            var page = pageNumber ?? 1;
            var size = pageSize ?? 10;

            var query = new GetRestaurantActiveOrdersQuery(restaurantId, page, size);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetRestaurantActiveOrders")
        .WithStandardResults<PaginatedList<OrderSummaryDto>>();
    }
}
