using YummyZoom.Web.Services.OrderFlowSimulator.Models;

namespace YummyZoom.Web.Services.OrderFlowSimulator;

public interface IOrderFlowSimulator
{
    Task<SimulationStartResult> StartAsync(Guid orderId, Guid restaurantId, SimulationRequest request, CancellationToken ct = default);
}

