using YummyZoom.Application.Common.Models;
using YummyZoom.Application.MenuCategories.Commands.AddMenuCategory;
using YummyZoom.Application.MenuCategories.Commands.RemoveMenuCategory;
using YummyZoom.Application.MenuCategories.Commands.UpdateMenuCategoryDetails;
using YummyZoom.Application.MenuCategories.Commands.ReorderMenuCategories;
using YummyZoom.Application.Menus.Commands.ChangeMenuAvailability;
using YummyZoom.Application.Menus.Commands.CreateMenu;
using YummyZoom.Application.Menus.Commands.RemoveMenu;
using YummyZoom.Application.Menus.Commands.UpdateMenuDetails;
using YummyZoom.Application.Restaurants.Queries.Management.GetMenuCategoriesForMenu;
using YummyZoom.Application.Restaurants.Queries.Management.GetMenuCategoryDetails;
using YummyZoom.Application.Restaurants.Queries.Management.GetMenuItemsByCategory;
using YummyZoom.Application.Restaurants.Queries.Management.SearchMenuItems;
using YummyZoom.Application.Restaurants.Queries.Management.GetMenusForManagement;

namespace YummyZoom.Web.Endpoints;

public partial class Restaurants
{
    private static void MapMenuManagement(IEndpointRouteBuilder group)
    {
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

        // GET /api/v1/restaurants/{restaurantId}/menus/{menuId}/categories
        group.MapGet("/{restaurantId:guid}/menus/{menuId:guid}/categories", async (Guid restaurantId, Guid menuId, ISender sender) =>
        {
            var result = await sender.Send(new GetMenuCategoriesForMenuQuery(restaurantId, menuId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetMenuCategoriesForMenu")
        .WithSummary("List menu categories for a menu")
        .WithDescription("Returns categories for a menu with display order and item counts. Requires restaurant staff authorization.")
        .Produces<IReadOnlyList<MenuCategorySummaryDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
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

        // GET /api/v1/restaurants/{restaurantId}/menu-items/search
        group.MapGet("/{restaurantId:guid}/menu-items/search", async (
            Guid restaurantId,
            string? q,
            Guid? categoryId,
            bool? isAvailable,
            int? pageNumber,
            int? pageSize,
            ISender sender) =>
        {
            var page = pageNumber ?? 1;
            var size = pageSize ?? 20;

            var query = new SearchMenuItemsQuery(restaurantId, categoryId, q, isAvailable, page, size);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("SearchMenuItems")
        .WithSummary("Search menu items across categories")
        .WithDescription("Returns paginated menu items for a restaurant filtered by optional name query, category, and availability. Defaults pageNumber=1 and pageSize=20.")
        .Produces<PaginatedList<MenuItemSearchResultDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

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

        // DELETE /api/v1/restaurants/{restaurantId}/menus/{menuId}
        group.MapDelete("/{restaurantId:guid}/menus/{menuId:guid}", async (Guid restaurantId, Guid menuId, ISender sender) =>
        {
            var cmd = new RemoveMenuCommand(restaurantId, menuId);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("RemoveMenu")
        .WithSummary("Remove a menu")
        .WithDescription("Permanently removes (soft deletes) a menu. Requires restaurant staff authorization.")
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

        // PUT /api/v1/restaurants/{restaurantId}/categories/reorder
        group.MapPut("/{restaurantId:guid}/categories/reorder", async (Guid restaurantId, ReorderMenuCategoriesRequestDto body, ISender sender) =>
        {
            var orders = body.CategoryOrders?.Select(o => new CategoryOrderDto(o.CategoryId, o.DisplayOrder)).ToList()
                ?? new List<CategoryOrderDto>();

            var cmd = new ReorderMenuCategoriesCommand(restaurantId, orders);
            var result = await sender.Send(cmd);
            return result.ToIResult();
        })
        .WithName("ReorderMenuCategories")
        .WithSummary("Reorder menu categories for a restaurant")
        .WithDescription("Bulk update of category display orders. Validates all provided categories belong to the restaurant before applying changes.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithStandardResults();
    }

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
    public sealed record ReorderMenuCategoriesRequestDto(List<CategoryOrderRequestDto>? CategoryOrders);
    public sealed record CategoryOrderRequestDto(Guid CategoryId, int DisplayOrder);
    #endregion
}
