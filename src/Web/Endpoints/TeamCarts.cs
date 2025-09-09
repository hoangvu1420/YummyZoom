using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Application.TeamCarts.Commands.JoinTeamCart;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.UpdateTeamCartItemQuantity;
using YummyZoom.Application.TeamCarts.Commands.RemoveItemFromTeamCart;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
using YummyZoom.Application.TeamCarts.Commands.ApplyTipToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.ApplyCouponToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.RemoveCouponFromTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment;
using YummyZoom.Application.TeamCarts.Commands.InitiateMemberOnlinePayment;
using YummyZoom.Application.TeamCarts.Commands.ConvertTeamCartToOrder;
using YummyZoom.Application.TeamCarts.Queries.GetTeamCartDetails;
using YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel;
using YummyZoom.Web.Infrastructure;
using YummyZoom.Web.Services;

namespace YummyZoom.Web.Endpoints;

public sealed class TeamCarts : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(this);

        // POST /api/v1/teamcarts
        group.MapPost("/", async (
            [FromBody] CreateTeamCartCommand command,
            ISender sender,
            ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var result = await sender.Send(command);
            return result.IsSuccess
                ? TypedResults.Created($"/api/v1/teamcarts/{result.Value.TeamCartId}", result.Value)
                : result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("CreateTeamCart")
        .WithSummary("Create a new TeamCart")
        .WithDescription("Creates a collaborative TeamCart for a restaurant and returns identifiers and share token details.")
        .WithStandardCreationResults<CreateTeamCartResponse>();

        // GET /api/v1/teamcarts/{id}
        group.MapGet("/{id}", async (Guid id, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var query = new GetTeamCartDetailsQuery(id);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("GetTeamCartDetails")
        .WithSummary("Get TeamCart details (SQL)")
        .WithDescription("Returns detailed TeamCart state including members, items, and payment status.")
        .WithStandardResults<GetTeamCartDetailsResponse>();

        // GET /api/v1/teamcarts/{id}/rt
        group.MapGet("/{id}/rt", async (Guid id, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var query = new GetTeamCartRealTimeViewModelQuery(id);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("GetTeamCartRealTimeViewModel")
        .WithSummary("Get TeamCart real-time view model (Redis)")
        .WithDescription("Returns the TeamCart view model from Redis for real-time updates.")
        .WithStandardResults<GetTeamCartRealTimeViewModelResponse>();

        // POST /api/v1/teamcarts/{id}/join
        group.MapPost("/{id}/join", async (Guid id, [FromBody] JoinTeamCartRequest body, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var command = new JoinTeamCartCommand(id, body.ShareToken, body.GuestName);
            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("JoinTeamCart")
        .WithSummary("Join a TeamCart")
        .WithDescription("Joins a TeamCart and returns the updated view model.")
        .WithStandardResults();

        // POST /api/v1/teamcarts/{id}/items
        group.MapPost("/{id}/items", async (Guid id, [FromBody] AddItemRequest body, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var command = new AddItemToTeamCartCommand(id, body.MenuItemId, body.Quantity, body.SelectedCustomizations);
            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("AddItemToTeamCart")
        .WithStandardResults();

        // PATCH /api/v1/teamcarts/{id}/items/{itemId}
        group.MapPatch("/{id}/items/{itemId}", async (Guid id, Guid itemId, [FromBody] UpdateItemQuantityRequest body, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var command = new UpdateTeamCartItemQuantityCommand(id, itemId, body.NewQuantity);
            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("UpdateTeamCartItemQuantity")
        .WithStandardResults();

        // DELETE /api/v1/teamcarts/{id}/items/{itemId}
        group.MapDelete("/{id}/items/{itemId}", async (Guid id, Guid itemId, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var command = new RemoveItemFromTeamCartCommand(id, itemId);
            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("RemoveItemFromTeamCart")
        .WithStandardResults();

        // POST /api/v1/teamcarts/{id}/lock
        group.MapPost("/{id}/lock", async (Guid id, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var command = new LockTeamCartForPaymentCommand(id);
            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("LockTeamCartForPayment")
        .WithStandardResults();

        // POST /api/v1/teamcarts/{id}/tip
        group.MapPost("/{id}/tip", async (Guid id, [FromBody] ApplyTipRequest body, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var command = new ApplyTipToTeamCartCommand(id, body.TipAmount);
            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("ApplyTipToTeamCart")
        .WithStandardResults();

        // POST /api/v1/teamcarts/{id}/coupon
        group.MapPost("/{id}/coupon", async (Guid id, [FromBody] ApplyCouponRequest body, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var command = new ApplyCouponToTeamCartCommand(id, body.CouponCode);
            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("ApplyCouponToTeamCart")
        .WithStandardResults();

        // DELETE /api/v1/teamcarts/{id}/coupon
        group.MapDelete("/{id}/coupon", async (Guid id, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var command = new RemoveCouponFromTeamCartCommand(id);
            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("RemoveCouponFromTeamCart")
        .WithStandardResults();

        // POST /api/v1/teamcarts/{id}/payments/cod
        group.MapPost("/{id}/payments/cod", async (Guid id, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var command = new CommitToCodPaymentCommand(id);
            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("CommitToCodPayment")
        .WithStandardResults();

        // POST /api/v1/teamcarts/{id}/payments/online
        group.MapPost("/{id}/payments/online", async (Guid id, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var command = new InitiateMemberOnlinePaymentCommand(id);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("InitiateMemberOnlinePayment")
        .WithStandardResults<InitiateMemberOnlinePaymentResponse>();

        // POST /api/v1/teamcarts/{id}/convert
        group.MapPost("/{id}/convert", async (Guid id, [FromBody] ConvertTeamCartRequest body, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var command = new ConvertTeamCartToOrderCommand(
                id,
                body.Street,
                body.City,
                body.State,
                body.ZipCode,
                body.Country,
                body.SpecialInstructions
            );
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("ConvertTeamCartToOrder")
        .WithStandardResults<ConvertTeamCartToOrderResponse>();
    }
}

// Request DTOs for TeamCarts endpoints (keep explicit for decoupling)
public sealed record JoinTeamCartRequest(string ShareToken, string GuestName);

public sealed record AddItemRequest(
    Guid MenuItemId,
    int Quantity,
    IReadOnlyList<AddItemToTeamCartCustomizationSelection>? SelectedCustomizations
);

public sealed record UpdateItemQuantityRequest(int NewQuantity);

public sealed record ApplyTipRequest(decimal TipAmount);

public sealed record ApplyCouponRequest(string CouponCode);

public sealed record ConvertTeamCartRequest(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country,
    string? SpecialInstructions
);
