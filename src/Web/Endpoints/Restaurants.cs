using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Application.Orders.Queries.GetRestaurantNewOrders;
using YummyZoom.Application.Orders.Queries.GetRestaurantActiveOrders;
using YummyZoom.Application.Restaurants.Queries.GetFullMenu;
using YummyZoom.Application.Restaurants.Queries.GetRestaurantPublicInfo;
using YummyZoom.Application.Restaurants.Queries.SearchRestaurants;
using YummyZoom.Application.MenuItems.Commands.CreateMenuItem;
using YummyZoom.Application.MenuItems.Commands.ChangeMenuItemAvailability;
using YummyZoom.Application.MenuItems.Commands.UpdateMenuItemDetails;
using YummyZoom.Application.MenuItems.Commands.AssignMenuItemToCategory;
using YummyZoom.Application.MenuItems.Commands.UpdateMenuItemDietaryTags;
using YummyZoom.Application.MenuItems.Commands.DeleteMenuItem;
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

        // Management endpoints (Restaurant staff scope)
        // POST /api/v1/restaurants/{restaurantId}/menu-items
        group.MapPost("/{restaurantId:guid}/menu-items", async (Guid restaurantId, CreateMenuItemRequestDto body, ISender sender) =>
        {
            var cmd = new CreateMenuItemCommand(
                RestaurantId: restaurantId,
                MenuCategoryId: body.MenuCategoryId,
                Name: body.Name,
                Description: body.Description,
                Price: body.Price,
                Currency: body.Currency,
                ImageUrl: body.ImageUrl,
                IsAvailable: body.IsAvailable,
                DietaryTagIds: body.DietaryTagIds);
            var result = await sender.Send(cmd);
            return result.IsSuccess
                ? Results.Created($"/api/v1/restaurants/{restaurantId}/menu-items/{result.Value.MenuItemId}", result.Value) 
                : result.ToIResult();
        })
        .WithName("CreateMenuItem")
        .WithSummary("Create a new menu item")
        .WithDescription("Creates a menu item within a restaurant and returns the created id.")
        .Produces<CreateMenuItemResponseDto>(StatusCodes.Status201Created);

        // PUT /api/v1/restaurants/{restaurantId}/menu-items/{itemId}/availability
        group.MapPut("/{restaurantId:guid}/menu-items/{itemId:guid}/availability", async (Guid restaurantId, Guid itemId, UpdateAvailabilityRequestDto body, ISender sender) =>
        {
            var cmd = new ChangeMenuItemAvailabilityCommand(restaurantId, itemId, body.IsAvailable);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("ChangeMenuItemAvailability")
        .WithSummary("Change menu item availability");

        // PUT /api/v1/restaurants/{restaurantId}/menu-items/{itemId}
        group.MapPut("/{restaurantId:guid}/menu-items/{itemId:guid}", async (Guid restaurantId, Guid itemId, UpdateMenuItemRequestDto body, ISender sender) =>
        {
            var cmd = new UpdateMenuItemDetailsCommand(
                RestaurantId: restaurantId,
                MenuItemId: itemId,
                Name: body.Name,
                Description: body.Description,
                Price: body.Price,
                Currency: body.Currency,
                ImageUrl: body.ImageUrl);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("UpdateMenuItemDetails")
        .WithSummary("Update menu item details");

        // PUT /api/v1/restaurants/{restaurantId}/menu-items/{itemId}/category
        group.MapPut("/{restaurantId:guid}/menu-items/{itemId:guid}/category", async (Guid restaurantId, Guid itemId, AssignMenuItemToCategoryRequestDto body, ISender sender) =>
        {
            var cmd = new AssignMenuItemToCategoryCommand(restaurantId, itemId, body.NewCategoryId);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("AssignMenuItemToCategory")
        .WithSummary("Assign a menu item to a category");

        // PUT /api/v1/restaurants/{restaurantId}/menu-items/{itemId}/dietary-tags
        group.MapPut("/{restaurantId:guid}/menu-items/{itemId:guid}/dietary-tags", async (Guid restaurantId, Guid itemId, UpdateDietaryTagsRequestDto body, ISender sender) =>
        {
            var cmd = new UpdateMenuItemDietaryTagsCommand(restaurantId, itemId, body.DietaryTagIds);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("UpdateMenuItemDietaryTags")
        .WithSummary("Update dietary tags for a menu item");

        // DELETE /api/v1/restaurants/{restaurantId}/menu-items/{itemId}
        group.MapDelete("/{restaurantId:guid}/menu-items/{itemId:guid}", async (Guid restaurantId, Guid itemId, ISender sender) =>
        {
            var cmd = new DeleteMenuItemCommand(restaurantId, itemId);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("DeleteMenuItem")
        .WithSummary("Delete a menu item");

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
        var publicGroup = app.MapGroup(this);

        // GET /api/v1/restaurants/{restaurantId}/menu
        publicGroup.MapGet("/{restaurantId:guid}/menu", async (Guid restaurantId, ISender sender, HttpContext http) =>
        {
            var result = await sender.Send(new GetFullMenuQuery(restaurantId));
            if (result.IsFailure) return result.ToIResult();

            var etag = HttpCaching.BuildWeakEtag(restaurantId, result.Value.LastRebuiltAt);
            var lastModified = HttpCaching.ToRfc1123(result.Value.LastRebuiltAt);

            if (HttpCaching.MatchesIfNoneMatch(http.Request, etag) ||
                HttpCaching.NotModifiedSince(http.Request, result.Value.LastRebuiltAt))
            {
                http.Response.Headers.ETag = etag.ToString();
                http.Response.Headers.LastModified = lastModified;
                http.Response.Headers.CacheControl = "public, max-age=300";
                return Results.StatusCode(StatusCodes.Status304NotModified);
            }

            http.Response.Headers.ETag = etag.ToString();
            http.Response.Headers.LastModified = lastModified;
            http.Response.Headers.CacheControl = "public, max-age=300";
            return Results.Text(result.Value.MenuJson, "application/json");
        })
        .WithName("GetRestaurantPublicMenu");

        // GET /api/v1/restaurants/{restaurantId}/info
        publicGroup.MapGet("/{restaurantId:guid}/info", async (Guid restaurantId, ISender sender) =>
        {
            var result = await sender.Send(new GetRestaurantPublicInfoQuery(restaurantId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetRestaurantPublicInfo");

        // GET /api/v1/restaurants/search
        publicGroup.MapGet("/search", async (string? q, string? cuisine, double? lat, double? lng, double? radiusKm, int pageNumber, int pageSize, ISender sender) =>
        {
            var query = new SearchRestaurantsQuery(q, cuisine, lat, lng, radiusKm, pageNumber, pageSize);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("SearchRestaurants");
    }

    // DTOs for management endpoints
    public sealed record CreateMenuItemRequestDto(Guid MenuCategoryId, string Name, string Description, decimal Price, string Currency, string? ImageUrl, bool IsAvailable, List<Guid>? DietaryTagIds);
    public sealed record CreateMenuItemResponseDto(Guid MenuItemId);
    public sealed record UpdateAvailabilityRequestDto(bool IsAvailable);
    public sealed record UpdateMenuItemRequestDto(string Name, string Description, decimal Price, string Currency, string? ImageUrl);
    public sealed record AssignMenuItemToCategoryRequestDto(Guid NewCategoryId);
    public sealed record UpdateDietaryTagsRequestDto(List<Guid>? DietaryTagIds);
}
