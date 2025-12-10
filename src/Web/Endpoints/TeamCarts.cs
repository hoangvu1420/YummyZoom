using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.ApplyCouponToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.ApplyTipToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment;
using YummyZoom.Application.TeamCarts.Commands.ConvertTeamCartToOrder;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Application.TeamCarts.Commands.FinalizePricing;
using YummyZoom.Application.TeamCarts.Commands.InitiateMemberOnlinePayment;
using YummyZoom.Application.TeamCarts.Commands.JoinTeamCart;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
using YummyZoom.Application.TeamCarts.Commands.RemoveCouponFromTeamCart;
using YummyZoom.Application.TeamCarts.Commands.RemoveItemFromTeamCart;
using YummyZoom.Application.TeamCarts.Commands.UpdateTeamCartItemQuantity;
using YummyZoom.Application.TeamCarts.Commands.SetMemberReady;
using YummyZoom.Application.Coupons.Queries.Common;
using YummyZoom.Application.TeamCarts.Queries.GetCouponSuggestions;
using YummyZoom.Application.TeamCarts.Queries.GetTeamCartDetails;
using YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel;
using YummyZoom.Web.Infrastructure;
using YummyZoom.Web.Services;
using YummyZoom.Application.TeamCarts.Queries.GetActiveTeamCart;

namespace YummyZoom.Web.Endpoints;

public sealed class TeamCarts : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(this);

        // POST /api/v1/team-carts
        group.MapPost("/", async (
            [FromBody] CreateTeamCartRequest request,
            HttpContext context,
            ISender sender,
            ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            
            // Extract idempotency key from header
            var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
            
            var command = new CreateTeamCartCommand(
                request.RestaurantId,
                request.HostName,
                request.DeadlineUtc,
                idempotencyKey);
                
            var result = await sender.Send(command);
            return result.IsSuccess
                ? TypedResults.Created($"/api/v1/team-carts/{result.Value.TeamCartId}", result.Value)
                : result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("CreateTeamCart")
        .WithSummary("Create a new TeamCart")
        .WithDescription("Creates a collaborative TeamCart for a restaurant and returns identifiers and share token details.")
        .WithStandardCreationResults<CreateTeamCartResponse>();

        // GET /api/v1/team-carts/active
        group.MapGet("/active", async (ISender sender) =>
        {
            var query = new GetActiveTeamCartQuery();
            var result = await sender.Send(query);

            if (result.IsFailure)
            {
                return result.ToIResult();
            }

            if (result.Value is null)
            {
                return Results.NoContent();
            }

            return Results.Ok(result.Value);
        })
        .RequireAuthorization()
        .WithName("GetActiveTeamCart")
        .WithSummary("Get active TeamCart summary")
        .WithDescription("Returns a lightweight summary of the user's active team cart, if any.")
        .Produces<GetActiveTeamCartResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status204NoContent);

        // GET /api/v1/team-carts/{id}
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

        // GET /api/v1/team-carts/{id}/rt
        group.MapGet("/{id}/rt", async (Guid id, HttpContext context, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var query = new GetTeamCartRealTimeViewModelQuery(id);
            var result = await sender.Send(query);
            if (!result.IsSuccess)
            {
                return result.ToIResult();
            }

            // Strong ETag based on VM Version: "teamcart-<id>-v<Version>"
            var vm = result.Value.TeamCart;
            var etag = $"\"teamcart-{id}-v{vm.Version}\""; // quoted strong ETag

            var ifNoneMatch = context.Request.Headers["If-None-Match"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(ifNoneMatch) && string.Equals(ifNoneMatch, etag, StringComparison.Ordinal))
            {
                context.Response.Headers.ETag = etag;
                context.Response.Headers.CacheControl = "no-cache, must-revalidate";
                return Results.StatusCode(StatusCodes.Status304NotModified);
            }

            context.Response.Headers.ETag = etag;
            context.Response.Headers.CacheControl = "no-cache, must-revalidate";
            context.Response.Headers.LastModified = DateTime.UtcNow.ToString("R");

            return Results.Ok(result.Value);
        })
        .RequireAuthorization()
        .WithName("GetTeamCartRealTimeViewModel")
        .WithSummary("Get TeamCart real-time view model (Redis)")
        .WithDescription("Returns the TeamCart view model from Redis for real-time updates.")
        .WithStandardResults<GetTeamCartRealTimeViewModelResponse>();

        // GET /api/v1/team-carts/{id}/coupon-suggestions
        group.MapGet("/{id}/coupon-suggestions", async (Guid id, ISender sender) =>
        {
            var query = new TeamCartCouponSuggestionsQuery(id);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("GetTeamCartCouponSuggestions")
        .WithSummary("Get coupon suggestions for TeamCart")
        .WithDescription("Returns applicable coupons with savings calculations for the current TeamCart items.")
        .WithStandardResults<CouponSuggestionsResponse>();

        // POST /api/v1/team-carts/{id}/join
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

        // POST /api/v1/team-carts/{id}/items
        group.MapPost("/{id}/items", async (Guid id, [FromBody] AddItemRequest body, HttpContext context, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            
            // Extract idempotency key from header
            var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
            
            var command = new AddItemToTeamCartCommand(id, body.MenuItemId, body.Quantity, body.SelectedCustomizations, idempotencyKey);
            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("AddItemToTeamCart")
        .WithStandardResults();

        // PUT /api/v1/team-carts/{id}/items/{itemId}
        group.MapPut("/{id}/items/{itemId}", async (Guid id, Guid itemId, [FromBody] UpdateItemQuantityRequest body, ISender sender, ITeamCartFeatureAvailability availability) =>
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

        // DELETE /api/v1/team-carts/{id}/items/{itemId}
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

        // POST /api/v1/team-carts/{id}/lock
        group.MapPost("/{id}/lock", async (Guid id, HttpContext context, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            
            // Extract idempotency key from header
            var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
            
            var command = new LockTeamCartForPaymentCommand(id, idempotencyKey);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("LockTeamCartForPayment")
        .WithSummary("Lock TeamCart for payment")
        .WithDescription("Locks the TeamCart for payment and returns the quote version for concurrency control.")
        .WithStandardResults<LockTeamCartForPaymentResponse>();

        // POST /api/v1/team-carts/{id}/finalize-pricing
        group.MapPost("/{id}/finalize-pricing", async (Guid id, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            
            var command = new FinalizePricingCommand(id);
            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("FinalizePricing")
        .WithSummary("Finalize pricing for TeamCart")
        .WithDescription("Finalizes pricing (tip and coupon), making them immutable and enabling member payments.")
        .WithStandardResults();

        // POST /api/v1/team-carts/{id}/tip
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

        // POST /api/v1/team-carts/{id}/coupon
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

        // DELETE /api/v1/team-carts/{id}/coupon
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

        // POST /api/v1/team-carts/{id}/payments/cod
        group.MapPost("/{id}/payments/cod", async (Guid id, [FromBody] CommitToCodPaymentRequest? body, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var command = new CommitToCodPaymentCommand(id, body?.QuoteVersion);
            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("CommitToCodPayment")
        .WithSummary("Commit to Cash on Delivery payment")
        .WithDescription("Commits the current user to pay cash on delivery for their portion of the TeamCart.")
        .WithStandardResults();

        // POST /api/v1/team-carts/{id}/payments/online
        group.MapPost("/{id}/payments/online", async (Guid id, [FromBody] InitiateMemberOnlinePaymentRequest? body, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var command = new InitiateMemberOnlinePaymentCommand(id, body?.QuoteVersion);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("InitiateMemberOnlinePayment")
        .WithSummary("Initiate online payment for TeamCart")
        .WithDescription("Initiates an online payment for the current user's portion of the TeamCart.")
        .WithStandardResults<InitiateMemberOnlinePaymentResponse>();

        // POST /api/v1/team-carts/{id}/convert
        group.MapPost("/{id}/convert", async (Guid id, [FromBody] ConvertTeamCartRequest body, HttpContext context, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            
            // Extract idempotency key from header
            var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
            
            var command = new ConvertTeamCartToOrderCommand(
                id,
                body.Street,
                body.City,
                body.State,
                body.ZipCode,
                body.Country,
                body.SpecialInstructions,
                idempotencyKey,
                body.QuoteVersion
            );
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("ConvertTeamCartToOrder")
        .WithStandardResults<ConvertTeamCartToOrderResponse>();

        // POST /api/v1/team-carts/{id}/ready
        group.MapPost("/{id}/ready", async (Guid id, [FromBody] SetMemberReadyRequest body, ISender sender, ITeamCartFeatureAvailability availability) =>
        {
            if (!availability.Enabled || !availability.RealTimeReady)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            var command = new SetMemberReadyCommand(id, body.IsReady);
            var result = await sender.Send(command);
            return result.ToIResult();
        })
        .RequireAuthorization()
        .WithName("SetMemberReady")
        .WithStandardResults();
    }
}

// Request DTOs for TeamCarts endpoints (keep explicit for decoupling)
public sealed record CreateTeamCartRequest(
    Guid RestaurantId,
    string HostName,
    DateTime? DeadlineUtc = null
);

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
    string? SpecialInstructions,
    long? QuoteVersion = null
);

public sealed record CommitToCodPaymentRequest(
    long? QuoteVersion = null
);

public sealed record InitiateMemberOnlinePaymentRequest(
    long? QuoteVersion = null
);

public sealed record SetMemberReadyRequest(bool IsReady);
