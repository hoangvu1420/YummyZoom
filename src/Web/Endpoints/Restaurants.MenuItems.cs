using System.Text.Json;
using YummyZoom.Application.MenuItems.Commands.AssignCustomizationGroupToMenuItem;
using YummyZoom.Application.MenuItems.Commands.AssignMenuItemToCategory;
using YummyZoom.Application.MenuItems.Commands.BatchUpdateMenuItems;
using YummyZoom.Application.MenuItems.Commands.ChangeMenuItemAvailability;
using YummyZoom.Application.MenuItems.Commands.CreateMenuItem;
using YummyZoom.Application.MenuItems.Commands.DeleteMenuItem;
using YummyZoom.Application.MenuItems.Commands.RemoveCustomizationGroupFromMenuItem;
using YummyZoom.Application.MenuItems.Commands.UpdateMenuItemDetails;
using YummyZoom.Application.MenuItems.Commands.UpdateMenuItemDietaryTags;
using YummyZoom.Application.MenuItems.Commands.UpdateMenuItemPrice;
using YummyZoom.Application.Restaurants.Queries.Management.GetMenuItemDetails;

namespace YummyZoom.Web.Endpoints;

public partial class Restaurants
{
    private static void MapMenuItemsManagement(IEndpointRouteBuilder group)
    {
        // GET /api/v1/restaurants/{restaurantId}/menu-items/{itemId}/management
        // Management-only details to avoid route ambiguity with public item details
        group.MapGet("/{restaurantId:guid}/menu-items/{itemId:guid}/management", async (Guid restaurantId, Guid itemId, ISender sender) =>
        {
            var result = await sender.Send(new GetMenuItemDetailsQuery(restaurantId, itemId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetMenuItemDetails")
        .WithSummary("Get menu item details (management)")
        .WithDescription("Returns full details for a specific menu item including tags and applied customizations. Management endpoint - requires restaurant staff authorization.")
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

        // POST /api/v1/restaurants/{restaurantId}/menu-items/batch-update
        group.MapPost("/{restaurantId:guid}/menu-items/batch-update", async (Guid restaurantId, BatchUpdateMenuItemsRequestDto body, ISender sender) =>
        {
            var operations = body.Operations?.Select(op => new MenuItemBatchUpdateOperation(op.ItemId, op.Field, op.Value)).ToList()
                ?? [];

            var cmd = new BatchUpdateMenuItemsCommand(
                RestaurantId: restaurantId,
                Operations: operations);

            var result = await sender.Send(cmd);
            if (!result.IsSuccess)
            {
                return result.ToIResult();
            }

            var response = new BatchUpdateMenuItemsResponseDto(
                result.Value.SuccessCount,
                result.Value.FailedCount,
                result.Value.Errors
                    .Select(e => new BatchUpdateMenuItemErrorDto(e.ItemId, e.Field, e.Message))
                    .ToList());

            return Results.Ok(response);
        })
        .WithName("BatchUpdateMenuItems")
        .WithSummary("Batch update menu item availability and price")
        .WithDescription("Updates availability or price for multiple menu items in a single request. Requires restaurant staff authorization.")
        .WithStandardResults<BatchUpdateMenuItemsResponseDto>();

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
    public sealed record BatchUpdateMenuItemsRequestDto(List<BatchMenuItemOperationRequestDto>? Operations);
    public sealed record BatchMenuItemOperationRequestDto(Guid ItemId, string Field, JsonElement Value);
    public sealed record BatchUpdateMenuItemsResponseDto(int SuccessCount, int FailedCount, IReadOnlyList<BatchUpdateMenuItemErrorDto> Errors);
    public sealed record BatchUpdateMenuItemErrorDto(Guid ItemId, string Field, string Message);
    #endregion
}
