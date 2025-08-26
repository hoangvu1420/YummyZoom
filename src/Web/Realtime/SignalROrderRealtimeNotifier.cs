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
/// Sends order lifecycle/payment updates to both restaurant and customer hub groups.
/// Supports targeting Restaurant, Customer, or Both via NotificationTarget parameter.
/// </summary>
public sealed class SignalROrderRealtimeNotifier : IOrderRealtimeNotifier
{
    private readonly IHubContext<RestaurantOrdersHub> _restaurantHubContext;
    private readonly IHubContext<CustomerOrdersHub> _customerHubContext;
    private readonly ILogger<SignalROrderRealtimeNotifier> _logger;

    public SignalROrderRealtimeNotifier(
        IHubContext<RestaurantOrdersHub> restaurantHubContext,
        IHubContext<CustomerOrdersHub> customerHubContext,
        ILogger<SignalROrderRealtimeNotifier> logger)
    {
        _restaurantHubContext = restaurantHubContext;
        _customerHubContext = customerHubContext;
        _logger = logger;
    }

    private static string RestaurantGroup(OrderStatusBroadcastDto dto) => $"restaurant:{dto.RestaurantId}";
    private static string CustomerGroup(OrderStatusBroadcastDto dto) => $"order:{dto.OrderId}";

    public async Task NotifyOrderPlaced(OrderStatusBroadcastDto dto, NotificationTarget target = NotificationTarget.Restaurant, CancellationToken cancellationToken = default)
    {
        await SendToTargetsAsync(dto, target, "ReceiveOrderPlaced", cancellationToken);
    }

    public async Task NotifyOrderPaymentSucceeded(OrderStatusBroadcastDto dto, NotificationTarget target = NotificationTarget.Restaurant, CancellationToken cancellationToken = default)
    {
        await SendToTargetsAsync(dto, target, "ReceiveOrderPaymentSucceeded", cancellationToken);
    }

    public async Task NotifyOrderPaymentFailed(OrderStatusBroadcastDto dto, NotificationTarget target = NotificationTarget.Restaurant, CancellationToken cancellationToken = default)
    {
        await SendToTargetsAsync(dto, target, "ReceiveOrderPaymentFailed", cancellationToken);
    }

    public async Task NotifyOrderStatusChanged(OrderStatusBroadcastDto dto, NotificationTarget target = NotificationTarget.Restaurant, CancellationToken cancellationToken = default)
    {
        await SendToTargetsAsync(dto, target, "ReceiveOrderStatusChanged", cancellationToken);
    }

    private async Task SendToTargetsAsync(OrderStatusBroadcastDto dto, NotificationTarget target, string signalRMethod, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        // Send to restaurant if target includes Restaurant
        if (target.HasFlag(NotificationTarget.Restaurant))
        {
            tasks.Add(SendToRestaurantAsync(dto, signalRMethod, cancellationToken));
        }

        // Send to customer if target includes Customer
        if (target.HasFlag(NotificationTarget.Customer))
        {
            tasks.Add(SendToCustomerAsync(dto, signalRMethod, cancellationToken));
        }

        // Execute all sends concurrently
        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    private async Task SendToRestaurantAsync(OrderStatusBroadcastDto dto, string signalRMethod, CancellationToken cancellationToken)
    {
        try
        {
            await _restaurantHubContext.Clients.Group(RestaurantGroup(dto)).SendAsync(signalRMethod, dto, cancellationToken);
            _logger.LogDebug("Broadcasted {Method} for OrderId={OrderId} to restaurant group {Group}", signalRMethod, dto.OrderId, RestaurantGroup(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast {Method} for OrderId={OrderId} to restaurant group {Group}", signalRMethod, dto.OrderId, RestaurantGroup(dto));
            // Don't rethrow to avoid breaking customer notifications
        }
    }

    private async Task SendToCustomerAsync(OrderStatusBroadcastDto dto, string signalRMethod, CancellationToken cancellationToken)
    {
        try
        {
            await _customerHubContext.Clients.Group(CustomerGroup(dto)).SendAsync(signalRMethod, dto, cancellationToken);
            _logger.LogDebug("Broadcasted {Method} for OrderId={OrderId} to customer group {Group}", signalRMethod, dto.OrderId, CustomerGroup(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast {Method} for OrderId={OrderId} to customer group {Group}", signalRMethod, dto.OrderId, CustomerGroup(dto));
            // Don't rethrow to avoid breaking restaurant notifications
        }
    }
}
