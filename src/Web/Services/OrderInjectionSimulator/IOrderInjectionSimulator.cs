using YummyZoom.Web.Services.OrderInjectionSimulator.Models;

namespace YummyZoom.Web.Services.OrderInjectionSimulator;

public interface IOrderInjectionSimulator
{
    Task<OrderInjectionResult> StartAsync(OrderInjectionRequest request, CancellationToken ct = default);
}
