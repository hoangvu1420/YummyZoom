using Microsoft.AspNetCore.Mvc;
using Dapper;
using Microsoft.Extensions.Options;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Web.Configuration;
using YummyZoom.Web.Services.OrderFlowSimulator;
using YummyZoom.Web.Services.OrderFlowSimulator.Models;

namespace YummyZoom.Web.Endpoints;

public sealed class DevOrders : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        // Intentionally not using MapGroup(this) to control the route to /dev/orders
        var group = app.MapGroup("/dev/orders");

        group.MapPost("/{orderId:guid}/simulate-flow", async (
            Guid orderId,
            [FromBody] SimulationRequest body,
            IDbConnectionFactory db,
            IOrderFlowSimulator simulator,
            IOptions<FeatureFlagsOptions> featureFlags,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            // Feature gate: only in Development/Test and when flag is enabled
            if (!(env.IsDevelopment() || env.IsEnvironment("Test")) || !featureFlags.Value.OrderFlowSimulation)
            {
                return Results.NotFound();
            }

            // Load order status and restaurant id directly (dev-only path) to avoid Application-layer auth
            using var conn = db.CreateConnection();
            const string sql = "SELECT \"RestaurantId\" AS RestaurantId, \"Status\" AS Status FROM \"Orders\" WHERE \"Id\" = @OrderId";
            var row = await conn.QuerySingleOrDefaultAsync<OrderMiniRow>(new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: ct));
            if (row is null)
            {
                return Results.NotFound();
            }

            // Must start at Placed or later (but not terminal)
            var status = (row.Status ?? string.Empty).Trim();
            if (string.Equals(status, "AwaitingPayment", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Conflict(new { code = "OrderNotPlaced", message = "Order must be at least Placed to simulate flow." });
            }
            if (string.Equals(status, "Delivered", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Conflict(new { code = "OrderTerminal", message = "Order is already in a terminal state." });
            }

            // Start simulation (no auth on endpoint; commands are impersonated inside simulator)
            try
            {
                var start = await simulator.StartAsync(orderId, row.RestaurantId, body, ct);
                return Results.Accepted($"/api/v1/dev/orders/{orderId}/simulation", start);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { code = "AlreadyRunning", message = ex.Message });
            }
        })
        .WithName("Dev_SimulateOrderFlow")
        .WithSummary("Start simulated order flow (dev only)")
        .WithDescription("Triggers a background simulation to advance an order through predefined status scenarios. Dev/Test only.")
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private sealed record OrderMiniRow(Guid RestaurantId, string Status);
}
