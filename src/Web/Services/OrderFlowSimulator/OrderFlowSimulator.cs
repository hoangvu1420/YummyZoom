using System.Collections.Concurrent;
using MediatR;
using YummyZoom.Application.Orders.Commands.AcceptOrder;
using YummyZoom.Application.Orders.Commands.CancelOrder;
using YummyZoom.Application.Orders.Commands.MarkOrderDelivered;
using YummyZoom.Application.Orders.Commands.MarkOrderPreparing;
using YummyZoom.Application.Orders.Commands.MarkOrderReadyForDelivery;
using YummyZoom.Application.Orders.Commands.RejectOrder;
using YummyZoom.Web.Security;
using YummyZoom.Web.Services.OrderFlowSimulator.Models;

namespace YummyZoom.Web.Services.OrderFlowSimulator;

public sealed class OrderFlowSimulator : IOrderFlowSimulator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDevImpersonationService _impersonation;

    private sealed record Run(Guid RunId, Guid OrderId, string Scenario, DateTime StartedAtUtc);
    private static readonly ConcurrentDictionary<Guid, Run> Runs = new();

    public OrderFlowSimulator(IServiceScopeFactory scopeFactory, IDevImpersonationService impersonation)
    {
        _scopeFactory = scopeFactory;
        _impersonation = impersonation;
    }

    public Task<SimulationStartResult> StartAsync(Guid orderId, Guid restaurantId, SimulationRequest request, CancellationToken ct = default)
    {
        if (Runs.ContainsKey(orderId))
        {
            throw new InvalidOperationException("A simulation is already running for this order.");
        }

        var run = new Run(Guid.NewGuid(), orderId, request.Scenario, DateTime.UtcNow);
        if (!Runs.TryAdd(orderId, run))
        {
            throw new InvalidOperationException("A simulation is already running for this order.");
        }

        _ = Task.Run(() => ExecuteAsync(run, restaurantId, request, ct), ct);

        return Task.FromResult(new SimulationStartResult
        {
            RunId = run.RunId,
            OrderId = orderId,
            Scenario = request.Scenario,
            Status = "Started",
            StartedAtUtc = run.StartedAtUtc,
            NextStep = NextStepForScenario(request.Scenario)
        });
    }

    private static string? NextStepForScenario(string scenario) => scenario switch
    {
        "rejected" => "Reject",
        _ => "Accept"
    };

    private async Task ExecuteAsync(Run run, Guid restaurantId, SimulationRequest request, CancellationToken ct)
    {
        try
        {
            var delays = BuildDelays(request);
            var acceptEta = TimeSpan.FromMinutes(request.EstimatedDeliveryMinutes ?? 40);

            switch ((request.Scenario ?? "happyPath").Trim().ToLowerInvariant())
            {
                case "fasthappypath":
                case "happypath":
                    await DelayAsync(delays.PlacedToAccepted, ct);
                    await AsStaffAsync(restaurantId, async sp =>
                    {
                        var sender = sp.GetRequiredService<ISender>();
                        await sender.Send(new AcceptOrderCommand(run.OrderId, restaurantId, DateTime.UtcNow.Add(acceptEta)), ct);
                    }, ct);

                    await DelayAsync(delays.AcceptedToPreparing, ct);
                    await AsStaffAsync(restaurantId, async sp =>
                    {
                        var sender = sp.GetRequiredService<ISender>();
                        await sender.Send(new MarkOrderPreparingCommand(run.OrderId, restaurantId), ct);
                    }, ct);

                    await DelayAsync(delays.PreparingToReady, ct);
                    await AsStaffAsync(restaurantId, async sp =>
                    {
                        var sender = sp.GetRequiredService<ISender>();
                        await sender.Send(new MarkOrderReadyForDeliveryCommand(run.OrderId, restaurantId), ct);
                    }, ct);

                    await DelayAsync(delays.ReadyToDelivered, ct);
                    await AsStaffAsync(restaurantId, async sp =>
                    {
                        var sender = sp.GetRequiredService<ISender>();
                        await sender.Send(new MarkOrderDeliveredCommand(run.OrderId, restaurantId, DateTime.UtcNow), ct);
                    }, ct);
                    break;

                case "rejected":
                    await DelayAsync(delays.PlacedToRejected, ct);
                    await AsStaffAsync(restaurantId, async sp =>
                    {
                        var sender = sp.GetRequiredService<ISender>();
                        await sender.Send(new RejectOrderCommand(run.OrderId, restaurantId, "Simulated"), ct);
                    }, ct);
                    break;

                case "cancelledbyrestaurant":
                    await DelayAsync(delays.PlacedToAccepted, ct);
                    await AsStaffAsync(restaurantId, async sp =>
                    {
                        var sender = sp.GetRequiredService<ISender>();
                        await sender.Send(new AcceptOrderCommand(run.OrderId, restaurantId, DateTime.UtcNow.Add(acceptEta)), ct);
                    }, ct);

                    await DelayAsync(delays.AcceptedToCancelled, ct);
                    await AsStaffAsync(restaurantId, async sp =>
                    {
                        var sender = sp.GetRequiredService<ISender>();
                        await sender.Send(new CancelOrderCommand(run.OrderId, restaurantId, null, "Simulated"), ct);
                    }, ct);
                    break;

                default:
                    // Fallback to happyPath
                    goto case "happypath";
            }
        }
        catch
        {
            // swallow in background; logs should be added if needed
        }
        finally
        {
            Runs.TryRemove(run.OrderId, out _);
        }
    }

    private async Task AsStaffAsync(Guid restaurantId, Func<IServiceProvider, Task> action, CancellationToken ct)
        => await _impersonation.RunAsRestaurantStaffAsync(restaurantId, _scopeFactory, action, ct);

    private static async Task DelayAsync(TimeSpan delay, CancellationToken ct)
    {
        if (delay <= TimeSpan.Zero) return;
        await Task.Delay(delay, ct);
    }

    private static (
        TimeSpan PlacedToAccepted,
        TimeSpan AcceptedToPreparing,
        TimeSpan PreparingToReady,
        TimeSpan ReadyToDelivered,
        TimeSpan PlacedToRejected,
        TimeSpan AcceptedToCancelled
        ) BuildDelays(SimulationRequest request)
    {
        var scenario = (request.Scenario ?? "happyPath").Trim().ToLowerInvariant();
        var fast = scenario == "fasthappypath";

        var defaults = new
        {
            PlacedToAccepted = fast ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(5),
            AcceptedToPreparing = fast ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(30),
            PreparingToReady = fast ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(45),
            ReadyToDelivered = fast ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(30),
            PlacedToRejected = fast ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(5),
            AcceptedToCancelled = fast ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(10)
        };

        var o = request.DelaysMs;
        return (
            o?.PlacedToAcceptedMs is > 0 ? TimeSpan.FromMilliseconds(o.PlacedToAcceptedMs.Value) : defaults.PlacedToAccepted,
            o?.AcceptedToPreparingMs is > 0 ? TimeSpan.FromMilliseconds(o.AcceptedToPreparingMs.Value) : defaults.AcceptedToPreparing,
            o?.PreparingToReadyMs is > 0 ? TimeSpan.FromMilliseconds(o.PreparingToReadyMs.Value) : defaults.PreparingToReady,
            o?.ReadyToDeliveredMs is > 0 ? TimeSpan.FromMilliseconds(o.ReadyToDeliveredMs.Value) : defaults.ReadyToDelivered,
            o?.PlacedToRejectedMs is > 0 ? TimeSpan.FromMilliseconds(o.PlacedToRejectedMs.Value) : defaults.PlacedToRejected,
            o?.AcceptedToCancelledMs is > 0 ? TimeSpan.FromMilliseconds(o.AcceptedToCancelledMs.Value) : defaults.AcceptedToCancelled
        );
    }
}
