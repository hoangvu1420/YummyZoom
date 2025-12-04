using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using YummyZoom.Web.Configuration;
using YummyZoom.Web.Services.TeamCartFlowSimulator;
using YummyZoom.Web.Services.TeamCartFlowSimulator.Models;

namespace YummyZoom.Web.Endpoints;

public sealed class DevTeamCarts : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dev/team-carts");

        group.MapPost("/simulate-full-flow", async (
            [FromBody] FullFlowSimulationRequest body,
            ITeamCartFlowSimulator simulator,
            IOptions<FeatureFlagsOptions> featureFlags,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            // Feature gate: only in Development/Test and when flag is enabled
            if (!(env.IsDevelopment() || env.IsEnvironment("Test")) || !featureFlags.Value.TeamCartFlowSimulation)
            {
                return Results.NotFound();
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(body.HostPhone))
            {
                return Results.BadRequest(new { code = "InvalidRequest", message = "HostPhone is required." });
            }

            if (body.MemberPhones == null || body.MemberPhones.Length == 0)
            {
                return Results.BadRequest(new { code = "InvalidRequest", message = "At least one member phone is required." });
            }

            try
            {
                var request = new SimulationRequest
                {
                    RestaurantId = body.RestaurantId,
                    Scenario = body.Scenario ?? "happyPath",
                    Mode = body.Mode ?? SimulationMode.Automatic,
                    DelaysMs = body.DelaysMs
                };

                var result = await simulator.SimulateFullFlowAsync(body.HostPhone, body.MemberPhones, request, ct);
                return Results.Accepted($"/dev/team-carts/{result.TeamCartId}/simulation", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { code = "SimulationError", message = ex.Message });
            }
        })
        .WithName("Dev_SimulateTeamCartFullFlow")
        .WithSummary("Simulate full team cart flow from creation to order (dev only)")
        .WithDescription("Creates a team cart, joins members, adds items, locks, processes payments, and converts to order. Dev/Test only.")
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{teamCartId:guid}/simulate-members", async (
            Guid teamCartId,
            [FromBody] MemberActionsSimulationRequest body,
            ITeamCartFlowSimulator simulator,
            IOptions<FeatureFlagsOptions> featureFlags,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            // Feature gate: only in Development/Test and when flag is enabled
            if (!(env.IsDevelopment() || env.IsEnvironment("Test")) || !featureFlags.Value.TeamCartFlowSimulation)
            {
                return Results.NotFound();
            }

            // Validate request
            if (body.MemberPhones == null || body.MemberPhones.Length == 0)
            {
                return Results.BadRequest(new { code = "InvalidRequest", message = "At least one member phone is required." });
            }

            try
            {
                var request = new SimulationRequest
                {
                    Scenario = body.Scenario ?? "memberCollaboration",
                    DelaysMs = body.DelaysMs
                };

                var result = await simulator.SimulateMemberActionsAsync(teamCartId, body.MemberPhones, request, ct);
                return Results.Accepted($"/dev/team-carts/{teamCartId}/simulation", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { code = "SimulationError", message = ex.Message });
            }
        })
        .WithName("Dev_SimulateTeamCartMemberActions")
        .WithSummary("Simulate member actions on existing team cart (dev only)")
        .WithDescription("Members join, add items, mark ready, and commit payments. Host actions are NOT simulated. Dev/Test only.")
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        // Manual control endpoints
        var simulationGroup = group.MapGroup("/{teamCartId:guid}/simulation");

        simulationGroup.MapPost("/members-join", async (
            Guid teamCartId,
            [FromBody] MembersJoinRequest? body,
            ITeamCartFlowSimulator simulator,
            IOptions<FeatureFlagsOptions> featureFlags,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            if (!(env.IsDevelopment() || env.IsEnvironment("Test")) || !featureFlags.Value.TeamCartFlowSimulation)
            {
                return Results.NotFound();
            }

            try
            {
                var result = await simulator.TriggerMembersJoinAsync(teamCartId, body?.DelayBetweenMembersMs, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { code = "SimulationError", message = ex.Message });
            }
        })
        .WithName("Dev_TriggerMembersJoin")
        .WithSummary("Trigger members to join team cart (dev only)")
        .WithDescription("Triggers members to join the team cart one-by-one with delays. Manual mode only.")
        .Produces<SimulationActionResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        simulationGroup.MapPost("/start-adding-items", async (
            Guid teamCartId,
            [FromBody] StartAddingItemsRequest? body,
            ITeamCartFlowSimulator simulator,
            IOptions<FeatureFlagsOptions> featureFlags,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            if (!(env.IsDevelopment() || env.IsEnvironment("Test")) || !featureFlags.Value.TeamCartFlowSimulation)
            {
                return Results.NotFound();
            }

            try
            {
                var result = await simulator.TriggerStartAddingItemsAsync(teamCartId, body?.DelayBetweenItemsMs, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { code = "SimulationError", message = ex.Message });
            }
        })
        .WithName("Dev_TriggerStartAddingItems")
        .WithSummary("Trigger item addition phase (dev only)")
        .WithDescription("Starts the item addition phase where members add items automatically with delays. Manual mode only.")
        .Produces<SimulationActionResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        simulationGroup.MapPost("/mark-ready", async (
            Guid teamCartId,
            ITeamCartFlowSimulator simulator,
            IOptions<FeatureFlagsOptions> featureFlags,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            if (!(env.IsDevelopment() || env.IsEnvironment("Test")) || !featureFlags.Value.TeamCartFlowSimulation)
            {
                return Results.NotFound();
            }

            try
            {
                var result = await simulator.TriggerMarkReadyAsync(teamCartId, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { code = "SimulationError", message = ex.Message });
            }
        })
        .WithName("Dev_TriggerMarkReady")
        .WithSummary("Mark all members as ready (dev only)")
        .WithDescription("Marks all members (including host) as ready. Manual mode only.")
        .Produces<SimulationActionResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        simulationGroup.MapPost("/lock", async (
            Guid teamCartId,
            ITeamCartFlowSimulator simulator,
            IOptions<FeatureFlagsOptions> featureFlags,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            if (!(env.IsDevelopment() || env.IsEnvironment("Test")) || !featureFlags.Value.TeamCartFlowSimulation)
            {
                return Results.NotFound();
            }

            try
            {
                var result = await simulator.TriggerLockAsync(teamCartId, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { code = "SimulationError", message = ex.Message });
            }
        })
        .WithName("Dev_TriggerLock")
        .WithSummary("Lock team cart for payment (dev only)")
        .WithDescription("Locks the team cart for payment (host action). Manual mode only.")
        .Produces<SimulationActionResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        simulationGroup.MapPost("/start-payments", async (
            Guid teamCartId,
            [FromBody] StartPaymentsRequest? body,
            ITeamCartFlowSimulator simulator,
            IOptions<FeatureFlagsOptions> featureFlags,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            if (!(env.IsDevelopment() || env.IsEnvironment("Test")) || !featureFlags.Value.TeamCartFlowSimulation)
            {
                return Results.NotFound();
            }

            try
            {
                var result = await simulator.TriggerStartPaymentsAsync(teamCartId, body?.DelayBetweenPaymentsMs, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { code = "SimulationError", message = ex.Message });
            }
        })
        .WithName("Dev_TriggerStartPayments")
        .WithSummary("Trigger payment phase (dev only)")
        .WithDescription("Starts the payment phase where members commit payments one-by-one with delays. Manual mode only.")
        .Produces<SimulationActionResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        simulationGroup.MapPost("/convert", async (
            Guid teamCartId,
            [FromBody] ConvertRequest? body,
            ITeamCartFlowSimulator simulator,
            IOptions<FeatureFlagsOptions> featureFlags,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            if (!(env.IsDevelopment() || env.IsEnvironment("Test")) || !featureFlags.Value.TeamCartFlowSimulation)
            {
                return Results.NotFound();
            }

            try
            {
                var result = await simulator.TriggerConvertAsync(teamCartId, body?.DeliveryAddress, body?.DeliveryNotes, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { code = "SimulationError", message = ex.Message });
            }
        })
        .WithName("Dev_TriggerConvert")
        .WithSummary("Convert team cart to order (dev only)")
        .WithDescription("Converts the team cart to an order (host action). Manual mode only.")
        .Produces<SimulationActionResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private sealed record FullFlowSimulationRequest(
        string HostPhone,
        string[] MemberPhones,
        Guid? RestaurantId,
        string? Scenario,
        SimulationMode? Mode,
        SimulationDelays? DelaysMs);

    private sealed record MembersJoinRequest(int? DelayBetweenMembersMs);

    private sealed record StartAddingItemsRequest(int? DelayBetweenItemsMs);

    private sealed record StartPaymentsRequest(int? DelayBetweenPaymentsMs);

    private sealed record ConvertRequest(DeliveryAddress? DeliveryAddress, string? DeliveryNotes);

    private sealed record MemberActionsSimulationRequest(
        string[] MemberPhones,
        string? Scenario,
        SimulationDelays? DelaysMs);
}
