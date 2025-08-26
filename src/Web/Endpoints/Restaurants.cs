using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Application.Orders.Queries.GetRestaurantNewOrders;
using YummyZoom.Application.Orders.Queries.GetRestaurantActiveOrders;
using YummyZoom.Application.Restaurants.Queries.GetFullMenu;
using YummyZoom.Application.Restaurants.Queries.GetRestaurantPublicInfo;
using YummyZoom.Application.Restaurants.Queries.SearchRestaurants;
using YummyZoom.Web.Infrastructure.Http;

namespace YummyZoom.Web.Endpoints;

/// <summary>
/// Restaurant-scoped query endpoints (order listings, etc.).
/// Scoped separately from <see cref="Orders"/> command endpoints to keep route surface organized.
/// Base route resolves to /api/v1/restaurants via versioned endpoint grouping.
/// </summary>
public class Restaurants : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup(this)
            .RequireAuthorization();

        // GET /api/v1/restaurants/{restaurantId}/orders/new
        group.MapGet("/{restaurantId:guid}/orders/new", async (Guid restaurantId, int pageNumber, int pageSize, ISender sender) =>
        {
            var query = new GetRestaurantNewOrdersQuery(restaurantId, pageNumber, pageSize);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetRestaurantNewOrders")
        .WithStandardResults<PaginatedList<OrderSummaryDto>>();

        // GET /api/v1/restaurants/{restaurantId}/orders/active
        group.MapGet("/{restaurantId:guid}/orders/active", async (Guid restaurantId, int pageNumber, int pageSize, ISender sender) =>
        {
            var query = new GetRestaurantActiveOrdersQuery(restaurantId, pageNumber, pageSize);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetRestaurantActiveOrders")
        .WithStandardResults<PaginatedList<OrderSummaryDto>>();

        // Public endpoints (no auth)
        var publicGroup = app.MapGroup("/api/restaurants");

        // GET /api/restaurants/{restaurantId}/menu
        publicGroup.MapGet("/{restaurantId:guid}/menu", async (Guid restaurantId, ISender sender, HttpContext http) =>
        {
            var result = await sender.Send(new GetFullMenuQuery(restaurantId));
            if (result.IsFailure) return result.ToIResult();

            var etag = HttpCaching.BuildWeakEtag(restaurantId, result.Value.LastRebuiltAt);
            var lastModified = HttpCaching.ToRfc1123(result.Value.LastRebuiltAt);

            if (HttpCaching.MatchesIfNoneMatch(http.Request, etag) ||
                HttpCaching.NotModifiedSince(http.Request, result.Value.LastRebuiltAt))
            {
                http.Response.Headers.ETag = etag;
                http.Response.Headers.LastModified = lastModified;
                http.Response.Headers.CacheControl = "public, max-age=300";
                return Results.StatusCode(StatusCodes.Status304NotModified);
            }

            http.Response.Headers.ETag = etag;
            http.Response.Headers.LastModified = lastModified;
            http.Response.Headers.CacheControl = "public, max-age=300";
            return Results.Text(result.Value.MenuJson, "application/json");
        })
        .WithName("GetRestaurantPublicMenu");

        // GET /api/restaurants/{restaurantId}/info
        publicGroup.MapGet("/{restaurantId:guid}/info", async (Guid restaurantId, ISender sender) =>
        {
            var result = await sender.Send(new GetRestaurantPublicInfoQuery(restaurantId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetRestaurantPublicInfo");

        // GET /api/restaurants/search
        publicGroup.MapGet("/search", async (string? q, string? cuisine, double? lat, double? lng, double? radiusKm, int pageNumber, int pageSize, ISender sender) =>
        {
            var query = new SearchRestaurantsQuery(q, cuisine, lat, lng, radiusKm, pageNumber, pageSize);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("SearchRestaurants");
    }
}
