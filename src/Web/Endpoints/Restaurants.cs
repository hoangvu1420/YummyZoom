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
using YummyZoom.Application.MenuItems.Commands.AssignCustomizationGroupToMenuItem;
using YummyZoom.Application.MenuItems.Commands.RemoveCustomizationGroupFromMenuItem;
using YummyZoom.Application.MenuItems.Commands.UpdateMenuItemPrice;
using YummyZoom.Application.Menus.Commands.CreateMenu;
using YummyZoom.Application.Menus.Commands.UpdateMenuDetails;
using YummyZoom.Application.Menus.Commands.ChangeMenuAvailability;
using YummyZoom.Application.MenuCategories.Commands.AddMenuCategory;
using YummyZoom.Application.MenuCategories.Commands.UpdateMenuCategoryDetails;
using YummyZoom.Application.MenuCategories.Commands.RemoveMenuCategory;
using YummyZoom.Web.Infrastructure.Http;
using YummyZoom.Application.Restaurants.Queries.Management.GetMenusForManagement;
using YummyZoom.Application.Restaurants.Queries.Management.GetMenuCategoryDetails;
using YummyZoom.Application.Restaurants.Queries.Management.GetMenuItemsByCategory;
using YummyZoom.Application.Restaurants.Queries.Management.GetMenuItemDetails;
using YummyZoom.Application.Reviews.Commands.CreateReview;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Application.Reviews.Commands.DeleteReview;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Restaurants.Commands.SetRestaurantAcceptingOrders;
using YummyZoom.Application.Reviews.Queries.GetRestaurantReviews;
using YummyZoom.Application.Reviews.Queries.Common;
using YummyZoom.Application.Reviews.Queries.GetRestaurantReviewSummary;
using YummyZoom.Application.Restaurants.Commands.UpdateRestaurantBusinessHours;
using YummyZoom.Application.Restaurants.Commands.UpdateRestaurantLocation;

namespace YummyZoom.Web.Endpoints;

/// <summary>
/// Restaurant-scoped endpoints for menu management, orders, and public information.
/// Includes menu hierarchy management (menus, categories, items) and restaurant operations.
/// Base route resolves to /api/v1/restaurants via versioned endpoint grouping.
/// </summary>
public class Restaurants : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup(this)
            .RequireAuthorization();

        #region Menu Management Endpoints (Restaurant Staff)
        
        // GET /api/v1/restaurants/{restaurantId}/menus
        group.MapGet("/{restaurantId:guid}/menus", async (Guid restaurantId, ISender sender) =>
        {
            var result = await sender.Send(new GetMenusForManagementQuery(restaurantId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetMenusForManagement")
        .WithSummary("List menus for management")
        .WithDescription("Returns all menus for a restaurant with counts of categories and items. Requires restaurant staff authorization.")
        .Produces<IReadOnlyList<MenuSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/v1/restaurants/{restaurantId}/categories/{categoryId}
        group.MapGet("/{restaurantId:guid}/categories/{categoryId:guid}", async (Guid restaurantId, Guid categoryId, ISender sender) =>
        {
            var result = await sender.Send(new GetMenuCategoryDetailsQuery(restaurantId, categoryId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetMenuCategoryDetails")
        .WithSummary("Get menu category details")
        .WithDescription("Returns details for a specific menu category and count of active items. Requires restaurant staff authorization.")
        .Produces<MenuCategoryDetailsDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/v1/restaurants/{restaurantId}/categories/{categoryId}/items
        group.MapGet("/{restaurantId:guid}/categories/{categoryId:guid}/items", async (
            Guid restaurantId,
            Guid categoryId,
            string? q,
            bool? isAvailable,
            int pageNumber,
            int pageSize,
            ISender sender) =>
        {
            var query = new GetMenuItemsByCategoryQuery(restaurantId, categoryId, q, isAvailable, pageNumber, pageSize);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetMenuItemsByCategory")
        .WithSummary("List menu items by category")
        .WithDescription("Returns paginated menu items within a specific category for a restaurant. Supports name and availability filters.")
        .Produces<PaginatedList<MenuItemSummaryDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/v1/restaurants/{restaurantId}/menu-items/{itemId}
        group.MapGet("/{restaurantId:guid}/menu-items/{itemId:guid}", async (Guid restaurantId, Guid itemId, ISender sender) =>
        {
            var result = await sender.Send(new GetMenuItemDetailsQuery(restaurantId, itemId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetMenuItemDetails")
        .WithSummary("Get menu item details")
        .WithDescription("Returns full details for a specific menu item including tags and applied customizations. Requires restaurant staff authorization.")
        .Produces<MenuItemDetailsDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
        
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
        .WithDescription("Creates a menu item within a restaurant and returns the created ID. Requires restaurant staff authorization.")
        .WithStandardCreationResults<CreateMenuItemResponseDto>();

        // PUT /api/v1/restaurants/{restaurantId}/menu-items/{itemId}/availability
        group.MapPut("/{restaurantId:guid}/menu-items/{itemId:guid}/availability", async (Guid restaurantId, Guid itemId, UpdateAvailabilityRequestDto body, ISender sender) =>
        {
            var cmd = new ChangeMenuItemAvailabilityCommand(restaurantId, itemId, body.IsAvailable);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("ChangeMenuItemAvailability")
        .WithSummary("Change menu item availability")
        .WithDescription("Updates the availability status of a menu item. Requires restaurant staff authorization.")
        .WithStandardResults();

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
        .WithSummary("Update menu item details")
        .WithDescription("Updates the name, description, price, currency, and image URL of a menu item. Requires restaurant staff authorization.")
        .WithStandardResults();

        // PUT /api/v1/restaurants/{restaurantId}/menu-items/{itemId}/category
        group.MapPut("/{restaurantId:guid}/menu-items/{itemId:guid}/category", async (Guid restaurantId, Guid itemId, AssignMenuItemToCategoryRequestDto body, ISender sender) =>
        {
            var cmd = new AssignMenuItemToCategoryCommand(restaurantId, itemId, body.NewCategoryId);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("AssignMenuItemToCategory")
        .WithSummary("Assign a menu item to a category")
        .WithDescription("Moves a menu item to a different category within the same restaurant. Requires restaurant staff authorization.")
        .WithStandardResults();

        // PUT /api/v1/restaurants/{restaurantId}/menu-items/{itemId}/dietary-tags
        group.MapPut("/{restaurantId:guid}/menu-items/{itemId:guid}/dietary-tags", async (Guid restaurantId, Guid itemId, UpdateDietaryTagsRequestDto body, ISender sender) =>
        {
            var cmd = new UpdateMenuItemDietaryTagsCommand(restaurantId, itemId, body.DietaryTagIds);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("UpdateMenuItemDietaryTags")
        .WithSummary("Update dietary tags for a menu item")
        .WithDescription("Updates the dietary tags (e.g., vegetarian, gluten-free) associated with a menu item. Requires restaurant staff authorization.")
        .WithStandardResults();

        // POST /api/v1/restaurants/{restaurantId}/menu-items/{itemId}/customizations
        group.MapPost("/{restaurantId:guid}/menu-items/{itemId:guid}/customizations", async (Guid restaurantId, Guid itemId, AssignCustomizationRequestDto body, ISender sender) =>
        {
            var cmd = new AssignCustomizationGroupToMenuItemCommand(
                RestaurantId: restaurantId,
                MenuItemId: itemId,
                CustomizationGroupId: body.GroupId,
                DisplayTitle: body.DisplayTitle,
                DisplayOrder: body.DisplayOrder);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("AssignCustomizationGroupToMenuItem")
        .WithSummary("Assign customization group to menu item")
        .WithDescription("Assigns a customization group to a menu item with optional display order. Requires restaurant staff authorization.")
        .WithStandardResults();

        // DELETE /api/v1/restaurants/{restaurantId}/menu-items/{itemId}/customizations/{groupId}
        group.MapDelete("/{restaurantId:guid}/menu-items/{itemId:guid}/customizations/{groupId:guid}", async (Guid restaurantId, Guid itemId, Guid groupId, ISender sender) =>
        {
            var cmd = new RemoveCustomizationGroupFromMenuItemCommand(
                RestaurantId: restaurantId,
                MenuItemId: itemId,
                CustomizationGroupId: groupId);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("RemoveCustomizationGroupFromMenuItem")
        .WithSummary("Remove customization group from menu item")
        .WithDescription("Removes a customization group assignment from a menu item. Requires restaurant staff authorization.")
        .WithStandardResults();

        // PUT /api/v1/restaurants/{restaurantId}/menu-items/{itemId}/price
        group.MapPut("/{restaurantId:guid}/menu-items/{itemId:guid}/price", async (Guid restaurantId, Guid itemId, UpdatePriceRequestDto body, ISender sender) =>
        {
            var cmd = new UpdateMenuItemPriceCommand(
                RestaurantId: restaurantId,
                MenuItemId: itemId,
                Price: body.Price,
                Currency: body.Currency);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("UpdateMenuItemPrice")
        .WithSummary("Update menu item price")
        .WithDescription("Updates the price and currency of a menu item. Requires restaurant staff authorization.")
        .WithStandardResults();

        // DELETE /api/v1/restaurants/{restaurantId}/menu-items/{itemId}
        group.MapDelete("/{restaurantId:guid}/menu-items/{itemId:guid}", async (Guid restaurantId, Guid itemId, ISender sender) =>
        {
            var cmd = new DeleteMenuItemCommand(restaurantId, itemId);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("DeleteMenuItem")
        .WithSummary("Delete a menu item")
        .WithDescription("Permanently removes a menu item from the restaurant's menu. Requires restaurant staff authorization.")
        .WithStandardResults();

        // Menu Management - Restaurant owners can create menus, staff can manage all aspects
        // POST /api/v1/restaurants/{restaurantId}/menus
        group.MapPost("/{restaurantId:guid}/menus", async (Guid restaurantId, CreateMenuRequestDto body, ISender sender) =>
        {
            var cmd = new CreateMenuCommand(
                RestaurantId: restaurantId,
                Name: body.Name,
                Description: body.Description,
                IsEnabled: body.IsEnabled);
            var result = await sender.Send(cmd);
            return result.IsSuccess
                ? Results.Created($"/api/v1/restaurants/{restaurantId}/menus/{result.Value.MenuId}", result.Value)
                : result.ToIResult();
        })
        .WithName("CreateMenu")
        .WithSummary("Create a new menu")
        .WithDescription("Creates a menu within a restaurant. Requires restaurant owner authorization.")
        .WithStandardCreationResults<CreateMenuResponseDto>();

        // PUT /api/v1/restaurants/{restaurantId}/menus/{menuId}
        group.MapPut("/{restaurantId:guid}/menus/{menuId:guid}", async (Guid restaurantId, Guid menuId, UpdateMenuDetailsRequestDto body, ISender sender) =>
        {
            var cmd = new UpdateMenuDetailsCommand(
                RestaurantId: restaurantId,
                MenuId: menuId,
                Name: body.Name,
                Description: body.Description);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("UpdateMenuDetails")
        .WithSummary("Update menu details")
        .WithDescription("Updates the name and description of a menu. Requires restaurant staff authorization.")
        .WithStandardResults();

        // PUT /api/v1/restaurants/{restaurantId}/menus/{menuId}/availability
        group.MapPut("/{restaurantId:guid}/menus/{menuId:guid}/availability", async (Guid restaurantId, Guid menuId, UpdateMenuAvailabilityRequestDto body, ISender sender) =>
        {
            var cmd = new ChangeMenuAvailabilityCommand(
                RestaurantId: restaurantId,
                MenuId: menuId,
                IsEnabled: body.IsEnabled);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("ChangeMenuAvailability")
        .WithSummary("Change menu availability")
        .WithDescription("Enables or disables a menu for customer ordering. Requires restaurant staff authorization.")
        .WithStandardResults();

        // Menu Category Management - Restaurant staff can manage categories within menus
        // POST /api/v1/restaurants/{restaurantId}/menus/{menuId}/categories
        group.MapPost("/{restaurantId:guid}/menus/{menuId:guid}/categories", async (Guid restaurantId, Guid menuId, AddMenuCategoryRequestDto body, ISender sender) =>
        {
            var cmd = new AddMenuCategoryCommand(
                RestaurantId: restaurantId,
                MenuId: menuId,
                Name: body.Name);
            var result = await sender.Send(cmd);
            return result.IsSuccess
                ? Results.Created($"/api/v1/restaurants/{restaurantId}/categories/{result.Value.MenuCategoryId}", result.Value)
                : result.ToIResult();
        })
        .WithName("AddMenuCategory")
        .WithSummary("Add a category to a menu")
        .WithDescription("Creates a menu category within a specific menu. Requires restaurant staff authorization.")
        .WithStandardCreationResults<AddMenuCategoryResponseDto>();

        // PUT /api/v1/restaurants/{restaurantId}/categories/{categoryId}
        group.MapPut("/{restaurantId:guid}/categories/{categoryId:guid}", async (Guid restaurantId, Guid categoryId, UpdateMenuCategoryDetailsRequestDto body, ISender sender) =>
        {
            var cmd = new UpdateMenuCategoryDetailsCommand(
                RestaurantId: restaurantId,
                MenuCategoryId: categoryId,
                Name: body.Name,
                DisplayOrder: body.DisplayOrder);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("UpdateMenuCategoryDetails")
        .WithSummary("Update menu category details")
        .WithDescription("Updates the name and display order of a menu category. Requires restaurant staff authorization.")
        .WithStandardResults();

        // DELETE /api/v1/restaurants/{restaurantId}/categories/{categoryId}
        group.MapDelete("/{restaurantId:guid}/categories/{categoryId:guid}", async (Guid restaurantId, Guid categoryId, ISender sender) =>
        {
            var cmd = new RemoveMenuCategoryCommand(
                RestaurantId: restaurantId,
                MenuCategoryId: categoryId);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("RemoveMenuCategory")
        .WithSummary("Remove a menu category")
        .WithDescription("Permanently removes a menu category. Cannot be deleted if it contains menu items. Requires restaurant staff authorization.")
        .WithStandardResults();

        // PUT /api/v1/restaurants/{restaurantId}/accepting-orders
        group.MapPut("/{restaurantId:guid}/accepting-orders", async (Guid restaurantId, SetAcceptingOrdersRequestDto body, ISender sender) =>
        {
            var cmd = new SetRestaurantAcceptingOrdersCommand(
                RestaurantId: restaurantId,
                IsAccepting: body.IsAccepting);
            var result = await sender.Send(cmd);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("SetRestaurantAcceptingOrders")
        .WithSummary("Set restaurant accepting orders switch")
        .WithDescription("Toggles whether the restaurant is currently accepting orders. Requires restaurant staff authorization.")
        .WithStandardResults<SetRestaurantAcceptingOrdersResponse>();

        // PUT /api/v1/restaurants/{restaurantId}/business-hours
        group.MapPut("/{restaurantId:guid}/business-hours", async (Guid restaurantId, UpdateBusinessHoursRequestDto body, ISender sender) =>
        {
            var cmd = new UpdateRestaurantBusinessHoursCommand(
                RestaurantId: restaurantId,
                BusinessHours: body.BusinessHours);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("UpdateRestaurantBusinessHours")
        .WithSummary("Update restaurant business hours")
        .WithDescription("Updates the restaurant business hours string. Requires restaurant staff authorization.")
        .WithStandardResults();

        // PUT /api/v1/restaurants/{restaurantId}/location
        group.MapPut("/{restaurantId:guid}/location", async (Guid restaurantId, UpdateLocationRequestDto body, ISender sender) =>
        {
            var cmd = new UpdateRestaurantLocationCommand(
                RestaurantId: restaurantId,
                Street: body.Street,
                City: body.City,
                State: body.State,
                ZipCode: body.ZipCode,
                Country: body.Country,
                Latitude: body.Latitude,
                Longitude: body.Longitude);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("UpdateRestaurantLocation")
        .WithSummary("Update restaurant address and optional geo coordinates")
        .WithDescription("Updates address fields and, if provided, updates geo coordinates. Requires restaurant staff authorization.")
        .WithStandardResults();

        #endregion

        #region Order Management Endpoints (Restaurant Staff)

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

        #endregion

        #region Customer Reviews

        // POST /api/v1/restaurants/{restaurantId}/reviews
        group.MapPost("/{restaurantId:guid}/reviews", async (Guid restaurantId, CreateReviewRequest body, ISender sender, IUser user) =>
        {
            var uid = user.DomainUserId ?? (user.Id is string sid && Guid.TryParse(sid, out var gid) ? UserId.Create(gid) : throw new UnauthorizedAccessException());
            var cmd = new CreateReviewCommand(
                OrderId: body.OrderId,
                RestaurantId: restaurantId,
                Rating: body.Rating,
                Title: body.Title,
                Comment: body.Comment)
            { UserId = uid };
            var result = await sender.Send(cmd);
            return result.IsSuccess 
                ? Results.Created($"/api/v1/restaurants/{restaurantId}/reviews/{result.Value.ReviewId}", new { reviewId = result.Value.ReviewId })
                : result.ToIResult();
        })
        .WithName("CreateReview")
        .WithSummary("Create a review for a delivered order at this restaurant")
        .WithDescription("Requires CompletedSignup; user must own the order and the order must be Delivered.")
        .WithStandardCreationResults<CreateReviewResponse>();

        // DELETE /api/v1/restaurants/{restaurantId}/reviews/{reviewId}
        group.MapDelete("/{restaurantId:guid}/reviews/{reviewId:guid}", async (Guid restaurantId, Guid reviewId, ISender sender, IUser user) =>
        {
            var uid = user.DomainUserId ?? (user.Id is string sid && Guid.TryParse(sid, out var gid) ? UserId.Create(gid) : throw new UnauthorizedAccessException());
            var cmd = new DeleteReviewCommand(reviewId) { UserId = uid };
            var result = await sender.Send(cmd);
            return result.IsSuccess
                ? Results.Ok()
                : result.ToIResult();
        })
        .WithName("DeleteReview")
        .WithSummary("Delete my review")
        .WithDescription("Requires CompletedSignup; user must own the review.")
        .WithStandardResults();

        #endregion

        #region Public Endpoints (No Authentication Required)

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
        .WithName("GetRestaurantPublicMenu")
        .WithSummary("Get restaurant's public menu")
        .WithDescription("Retrieves the complete menu for a restaurant including categories and items. Public endpoint - no authentication required. Supports HTTP caching with ETag and Last-Modified headers.")
        .Produces<string>(StatusCodes.Status200OK, "application/json")
        .Produces(StatusCodes.Status304NotModified)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);


        // GET /api/v1/restaurants/{restaurantId}/reviews
        publicGroup.MapGet("/{restaurantId:guid}/reviews", async (Guid restaurantId, int pageNumber, int pageSize, ISender sender) =>
        {
            var result = await sender.Send(new GetRestaurantReviewsQuery(restaurantId, pageNumber, pageSize));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetRestaurantReviews")
        .WithSummary("List public reviews for this restaurant")
        .Produces<PaginatedList<ReviewDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/v1/restaurants/{restaurantId}/reviews/summary
        publicGroup.MapGet("/{restaurantId:guid}/reviews/summary", async (Guid restaurantId, ISender sender) =>
        {
            var result = await sender.Send(new GetRestaurantReviewSummaryQuery(restaurantId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetRestaurantReviewSummary")
        .WithSummary("Get review summary for this restaurant")
        .Produces<RestaurantReviewSummaryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/v1/restaurants/{restaurantId}/info
        publicGroup.MapGet("/{restaurantId:guid}/info", async (Guid restaurantId, ISender sender) =>
        {
            var result = await sender.Send(new GetRestaurantPublicInfoQuery(restaurantId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetRestaurantPublicInfo")
        .WithSummary("Get restaurant's public information")
        .WithDescription("Retrieves basic public information about a restaurant such as name, address, and contact details. Public endpoint - no authentication required.")
        .Produces<object>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/v1/restaurants/search
        publicGroup.MapGet("/search", async (string? q, string? cuisine, double? lat, double? lng, double? radiusKm, int pageNumber, int pageSize, double? minRating, ISender sender) =>
        {
            var query = new SearchRestaurantsQuery(q, cuisine, lat, lng, radiusKm, pageNumber, pageSize, minRating);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("SearchRestaurants")
        .WithSummary("Search restaurants")
        .WithDescription("Searches for restaurants by name, cuisine type, and/or location with optional radius filtering. Returns paginated results. Public endpoint - no authentication required.")
        .Produces<object>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        #endregion
    }

    #region DTOs for Menu Item Management
    public sealed record CreateMenuItemRequestDto(Guid MenuCategoryId, string Name, string Description, decimal Price, string Currency, string? ImageUrl, bool IsAvailable, List<Guid>? DietaryTagIds);
    public sealed record CreateMenuItemResponseDto(Guid MenuItemId);
    public sealed record UpdateAvailabilityRequestDto(bool IsAvailable);
    public sealed record UpdateMenuItemRequestDto(string Name, string Description, decimal Price, string Currency, string? ImageUrl);
    public sealed record AssignMenuItemToCategoryRequestDto(Guid NewCategoryId);
    public sealed record UpdateDietaryTagsRequestDto(List<Guid>? DietaryTagIds);
    public sealed record AssignCustomizationRequestDto(Guid GroupId, string DisplayTitle, int? DisplayOrder);
    public sealed record UpdatePriceRequestDto(decimal Price, string Currency);

    #endregion

    #region DTOs for Menu Management

    public sealed record CreateMenuRequestDto(string Name, string Description, bool IsEnabled = true);
    public sealed record CreateMenuResponseDto(Guid MenuId);
    public sealed record UpdateMenuDetailsRequestDto(string Name, string Description);
    public sealed record UpdateMenuAvailabilityRequestDto(bool IsEnabled);

    #endregion

    #region DTOs for Menu Category Management

    public sealed record AddMenuCategoryRequestDto(string Name);
    public sealed record AddMenuCategoryResponseDto(Guid MenuCategoryId);
    public sealed record UpdateMenuCategoryDetailsRequestDto(string Name, int DisplayOrder);

    #endregion

    #region DTOs for Reviews
    public sealed record CreateReviewRequest(Guid OrderId, int Rating, string? Title, string? Comment);
    #endregion

    #region DTOs for Restaurant Settings
    public sealed record SetAcceptingOrdersRequestDto(bool IsAccepting);
    public sealed record UpdateBusinessHoursRequestDto(string BusinessHours);
    public sealed record UpdateLocationRequestDto(
        string Street,
        string City,
        string State,
        string ZipCode,
        string Country,
        double? Latitude,
        double? Longitude);
    #endregion
}
