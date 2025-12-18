using YummyZoom.Application.CustomizationGroups.Commands.AddCustomizationChoice;
using YummyZoom.Application.CustomizationGroups.Commands.CreateCustomizationGroup;
using YummyZoom.Application.CustomizationGroups.Commands.DeleteCustomizationGroup;
using YummyZoom.Application.CustomizationGroups.Commands.RemoveCustomizationChoice;
using YummyZoom.Application.CustomizationGroups.Commands.ReorderCustomizationChoices;
using YummyZoom.Application.CustomizationGroups.Commands.UpdateCustomizationChoice;
using YummyZoom.Application.CustomizationGroups.Commands.UpdateCustomizationGroup;
using YummyZoom.Application.CustomizationGroups.Queries.GetCustomizationGroupDetails;
using YummyZoom.Application.CustomizationGroups.Queries.GetCustomizationGroups;

namespace YummyZoom.Web.Endpoints;

public partial class Restaurants
{
    private static void MapCustomizationGroups(IEndpointRouteBuilder group)
    {
        // POST /api/v1/restaurants/{restaurantId}/customization-groups/{groupId}/choices/reorder
        group.MapPost("/{restaurantId:guid}/customization-groups/{groupId:guid}/choices/reorder", async (
            Guid restaurantId,
            Guid groupId,
            ReorderCustomizationChoicesRequestDto body,
            ISender sender) =>
        {
            var choiceOrders = body.ChoiceOrders
                .Select(x => new ChoiceOrderDto(x.ChoiceId, x.DisplayOrder))
                .ToList();

            var command = new ReorderCustomizationChoicesCommand(
                restaurantId,
                groupId,
                choiceOrders);

            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .WithName("ReorderCustomizationChoices")
        .WithSummary("Reorder customization choices")
        .WithDescription("Batch updates the display order of customization choices. Requires restaurant staff authorization.")
        .WithStandardResults();

        // GET /api/v1/restaurants/{restaurantId}/customization-groups
        group.MapGet("/{restaurantId:guid}/customization-groups", async (Guid restaurantId, ISender sender) =>
        {
            var query = new GetCustomizationGroupsQuery(restaurantId);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("ListCustomizationGroups")
        .WithSummary("List customization groups")
        .WithDescription("Returns all customization groups for a restaurant. Requires restaurant staff authorization.")
        .Produces<List<CustomizationGroupSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/v1/restaurants/{restaurantId}/customization-groups/{groupId}
        group.MapGet("/{restaurantId:guid}/customization-groups/{groupId:guid}", async (Guid restaurantId, Guid groupId, ISender sender) =>
        {
            var query = new GetCustomizationGroupDetailsQuery(restaurantId, groupId);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetCustomizationGroupDetails")
        .WithSummary("Get customization group details")
        .WithDescription("Returns details for a specific customization group with its choices. Requires restaurant staff authorization.")
        .Produces<CustomizationGroupDetailsDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // POST /api/v1/restaurants/{restaurantId}/customization-groups
        group.MapPost("/{restaurantId:guid}/customization-groups", async (Guid restaurantId, CreateCustomizationGroupRequestDto body, ISender sender) =>
        {
            var command = new CreateCustomizationGroupCommand(
                restaurantId,
                body.Name,
                body.MinSelections,
                body.MaxSelections);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Created($"/api/v1/restaurants/{restaurantId}/customization-groups/{result.Value}", new { Id = result.Value })
                : result.ToIResult();
        })
        .WithName("CreateCustomizationGroup")
        .WithSummary("Create customization group")
        .WithDescription("Creates a new customization group. Requires restaurant staff authorization.")
        .WithStandardCreationResults<Guid>();

        // PUT /api/v1/restaurants/{restaurantId}/customization-groups/{groupId}
        group.MapPut("/{restaurantId:guid}/customization-groups/{groupId:guid}", async (
            Guid restaurantId,
            Guid groupId,
            UpdateCustomizationGroupRequestDto body,
            ISender sender) =>
        {
            var command = new UpdateCustomizationGroupCommand(
                restaurantId,
                groupId,
                body.Name,
                body.MinSelections,
                body.MaxSelections);

            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .WithName("UpdateCustomizationGroup")
        .WithSummary("Update customization group")
        .WithDescription("Updates a customization group's details. Requires restaurant staff authorization.")
        .WithStandardResults();

        // DELETE /api/v1/restaurants/{restaurantId}/customization-groups/{groupId}
        group.MapDelete("/{restaurantId:guid}/customization-groups/{groupId:guid}", async (
            Guid restaurantId,
            Guid groupId,
            ISender sender) =>
        {
            var command = new DeleteCustomizationGroupCommand(restaurantId, groupId);
            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .WithName("DeleteCustomizationGroup")
        .WithSummary("Delete customization group")
        .WithDescription("Soft-deletes a customization group. Requires restaurant staff authorization.")
        .WithStandardResults();

        // POST /api/v1/restaurants/{restaurantId}/customization-groups/{groupId}/choices
        group.MapPost("/{restaurantId:guid}/customization-groups/{groupId:guid}/choices", async (
            Guid restaurantId,
            Guid groupId,
            AddCustomizationChoiceRequestDto body,
            ISender sender) =>
        {
            var command = new AddCustomizationChoiceCommand(
                restaurantId,
                groupId,
                body.Name,
                body.PriceAdjustment,
                body.Currency,
                body.IsDefault,
                body.DisplayOrder);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Created($"/api/v1/restaurants/{restaurantId}/customization-groups/{groupId}/choices/{result.Value}", new { Id = result.Value })
                : result.ToIResult();
        })
        .WithName("AddCustomizationChoice")
        .WithSummary("Add customization choice")
        .WithDescription("Adds a choice to a customization group. Requires restaurant staff authorization.")
        .WithStandardCreationResults<Guid>();

        // PUT /api/v1/restaurants/{restaurantId}/customization-groups/{groupId}/choices/{choiceId}
        group.MapPut("/{restaurantId:guid}/customization-groups/{groupId:guid}/choices/{choiceId:guid}", async (
            Guid restaurantId,
            Guid groupId,
            Guid choiceId,
            UpdateCustomizationChoiceRequestDto body,
            ISender sender) =>
        {
            var command = new UpdateCustomizationChoiceCommand(
                restaurantId,
                groupId,
                choiceId,
                body.Name,
                body.PriceAdjustment,
                body.Currency,
                body.IsDefault,
                body.DisplayOrder);

            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .WithName("UpdateCustomizationChoice")
        .WithSummary("Update customization choice")
        .WithDescription("Updates a customization choice. Requires restaurant staff authorization.")
        .WithStandardResults();

        // DELETE /api/v1/restaurants/{restaurantId}/customization-groups/{groupId}/choices/{choiceId}
        group.MapDelete("/{restaurantId:guid}/customization-groups/{groupId:guid}/choices/{choiceId:guid}", async (
            Guid restaurantId,
            Guid groupId,
            Guid choiceId,
            ISender sender) =>
        {
            var command = new RemoveCustomizationChoiceCommand(
                restaurantId,
                groupId,
                choiceId);

            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .WithName("RemoveCustomizationChoice")
        .WithSummary("Remove customization choice")
        .WithDescription("Removes a choice from a customization group. Requires restaurant staff authorization.")
        .WithStandardResults();
    }

    public sealed record CreateCustomizationGroupRequestDto(string Name, int MinSelections, int MaxSelections);
    public sealed record UpdateCustomizationGroupRequestDto(string Name, int MinSelections, int MaxSelections);
    public sealed record AddCustomizationChoiceRequestDto(string Name, decimal PriceAdjustment, string Currency, bool IsDefault, int? DisplayOrder);
    public sealed record UpdateCustomizationChoiceRequestDto(string Name, decimal PriceAdjustment, string Currency, bool IsDefault, int? DisplayOrder);
    public sealed record ReorderCustomizationChoicesRequestDto(List<ChoiceOrderRequestDto> ChoiceOrders);
    public sealed record ChoiceOrderRequestDto(Guid ChoiceId, int DisplayOrder);
}
