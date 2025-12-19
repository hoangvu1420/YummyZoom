using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using YummyZoom.Web.Configuration;
using YummyZoom.Web.Services.OrderInjectionSimulator;
using YummyZoom.Web.Services.OrderInjectionSimulator.Models;

namespace YummyZoom.Web.Endpoints;

public sealed class DevOrderSimulator : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dev/order-simulator");

        group.MapPost("/start", async (
            [FromBody] OrderInjectionRequest body,
            IOrderInjectionSimulator simulator,
            IOptions<FeatureFlagsOptions> featureFlags,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            if (!(env.IsDevelopment() || env.IsEnvironment("Test")) || !featureFlags.Value.OrderFlowSimulation)
            {
                return Results.NotFound();
            }

            try
            {
                var result = await simulator.StartAsync(body, ct);
                return Results.Accepted("/dev/order-simulator/start", result);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new { code = "InvalidRequest", message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { code = "InvalidRequest", message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { code = "SimulationFailed", message = ex.Message });
            }
        })
        .WithName("Dev_OrderSimulator_Start")
        .WithSummary("Create simulated incoming orders for a restaurant (dev/test only)")
        .WithDescription("Creates synthetic orders for a restaurant and optionally auto-advances them through the restaurant flow. Dev/Test + feature flag only.")
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
