using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Orders.Broadcasting;
using YummyZoom.Web.Realtime.Hubs;

namespace YummyZoom.Web.Realtime;

/// <summary>
/// SignalR-backed implementation of IOrderRealtimeNotifier.
/// Sends order lifecycle/payment updates to restaurant group subscribers.
/// </summary>
public sealed class SignalROrderRealtimeNotifier : IOrderRealtimeNotifier
{
    private readonly IHubContext<RestaurantOrdersHub> _hubContext;
    private readonly ILogger<SignalROrderRealtimeNotifier> _logger;

    public SignalROrderRealtimeNotifier(
        IHubContext<RestaurantOrdersHub> hubContext,
        ILogger<SignalROrderRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    private static string Group(OrderStatusBroadcastDto dto) => $"restaurant:{dto.RestaurantId}";

    public async Task NotifyOrderPlaced(OrderStatusBroadcastDto dto, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(Group(dto)).SendAsync("ReceiveOrderPlaced", dto, cancellationToken);
        _logger.LogDebug("Broadcasted ReceiveOrderPlaced for OrderId={OrderId} to {Group}", dto.OrderId, Group(dto));
    }

    public async Task NotifyOrderPaymentSucceeded(OrderStatusBroadcastDto dto, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(Group(dto)).SendAsync("ReceiveOrderPaymentSucceeded", dto, cancellationToken);
        _logger.LogDebug("Broadcasted ReceiveOrderPaymentSucceeded for OrderId={OrderId} to {Group}", dto.OrderId, Group(dto));
    }

    public async Task NotifyOrderPaymentFailed(OrderStatusBroadcastDto dto, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(Group(dto)).SendAsync("ReceiveOrderPaymentFailed", dto, cancellationToken);
        _logger.LogDebug("Broadcasted ReceiveOrderPaymentFailed for OrderId={OrderId} to {Group}", dto.OrderId, Group(dto));
    }

    public async Task NotifyOrderStatusChanged(OrderStatusBroadcastDto dto, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(Group(dto)).SendAsync("ReceiveOrderStatusChanged", dto, cancellationToken);
        _logger.LogDebug("Broadcasted ReceiveOrderStatusChanged for OrderId={OrderId} to {Group}", dto.OrderId, Group(dto));
    }
}
