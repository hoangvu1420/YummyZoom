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
    }

    private sealed record FullFlowSimulationRequest(
        string HostPhone,
        string[] MemberPhones,
        Guid? RestaurantId,
        string? Scenario,
        SimulationDelays? DelaysMs);

    private sealed record MemberActionsSimulationRequest(
        string[] MemberPhones,
        string? Scenario,
        SimulationDelays? DelaysMs);
}
