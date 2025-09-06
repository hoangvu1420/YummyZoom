using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace YummyZoom.Web.Realtime.Hubs;

/// <summary>
/// Hub for TeamCart collaboration updates. Behind feature flag.
/// Authorization: requires authenticated user; membership enforcement can be added in subsequent phases.
/// </summary>
[Authorize]
public sealed class TeamCartHub : Hub
{
    private readonly ILogger<TeamCartHub> _logger;

    public TeamCartHub(ILogger<TeamCartHub> logger)
    {
        _logger = logger;
    }

    private static string Group(Guid cartId) => $"teamcart:{cartId}";

    public async Task SubscribeToCart(Guid cartId)
    {
        var user = Context.User;
        if (user is null)
        {
            throw new HubException("Unauthorized");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, Group(cartId));
        _logger.LogInformation("Subscribed connection {ConnectionId} to {Group}", Context.ConnectionId, Group(cartId));
    }

    public async Task UnsubscribeFromCart(Guid cartId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(cartId));
        _logger.LogInformation("Unsubscribed connection {ConnectionId} from {Group}", Context.ConnectionId, Group(cartId));
    }
}

