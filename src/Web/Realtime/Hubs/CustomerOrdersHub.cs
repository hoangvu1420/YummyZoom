using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Web.Realtime.Hubs;

/// <summary>
/// Hub for customers to receive order-specific lifecycle/payment updates.
/// Authorization: requires authenticated user; subscription is resource-checked per order.
/// </summary>
[Authorize]
public sealed class CustomerOrdersHub : Hub
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<CustomerOrdersHub> _logger;

    public CustomerOrdersHub(
        IAuthorizationService authorizationService,
        ILogger<CustomerOrdersHub> logger)
    {
        _authorizationService = authorizationService;
        _logger = logger;
    }

    private static string Group(Guid orderId) => $"order:{orderId}";

    private sealed record OrderResource(Guid Id) : IOrderCommand
    {
        public OrderId OrderId => OrderId.Create(Id);
    }

    /// <summary>
    /// Authorizes the caller and subscribes the current connection to the order group.
    /// </summary>
    public async Task SubscribeToOrder(Guid orderId)
    {
        var user = Context.User;
        if (user is null)
        {
            throw new HubException("Unauthorized");
        }

        // Prefer policy-based authorization if wired
        var resource = new OrderResource(orderId);
        var result = await _authorizationService.AuthorizeAsync(user, resource, Policies.MustBeOrderOwner);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Hub Subscribe forbidden: UserId={UserId}, OrderId={OrderId}",
                user.Identity?.Name ?? "anonymous", orderId);
            throw new HubException("Forbidden");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, Group(orderId));
        _logger.LogInformation("Subscribed connection {ConnectionId} to {Group}", Context.ConnectionId, Group(orderId));
    }

    /// <summary>
    /// Removes the current connection from the order group.
    /// </summary>
    public async Task UnsubscribeFromOrder(Guid orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(orderId));
        _logger.LogInformation("Unsubscribed connection {ConnectionId} from {Group}", Context.ConnectionId, Group(orderId));
    }
}


