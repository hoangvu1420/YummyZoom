using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Web.Realtime.Hubs;

/// <summary>
/// Hub for restaurant staff dashboards to receive order lifecycle/payment updates.
/// Authorization: requires authenticated user; subscription is resource-checked per restaurant using cached claims.
/// </summary>
[Authorize]
public sealed class RestaurantOrdersHub : Hub
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<RestaurantOrdersHub> _logger;

    public RestaurantOrdersHub(
        IAuthorizationService authorizationService,
        ILogger<RestaurantOrdersHub> logger)
    {
        _authorizationService = authorizationService;
        _logger = logger;
    }

    private static string Group(Guid restaurantId) => $"restaurant:{restaurantId}";

    private sealed record RestaurantResource(Guid Id) : IRestaurantCommand
    {
        public RestaurantId RestaurantId => RestaurantId.Create(Id);
    }

    public async Task SubscribeToRestaurant(Guid restaurantId)
    {
        var user = Context.User;
        if (user is null)
        {
            throw new HubException("Unauthorized");
        }

        var resource = new RestaurantResource(restaurantId);

        var owner = await _authorizationService.AuthorizeAsync(user, resource, Policies.MustBeRestaurantOwner);
        var staff = owner.Succeeded
            ? owner
            : await _authorizationService.AuthorizeAsync(user, resource, Policies.MustBeRestaurantStaff);

        if (!staff.Succeeded)
        {
            _logger.LogWarning("Hub Subscribe forbidden: UserId={UserId}, RestaurantId={RestaurantId}",
                user.Identity?.Name ?? "anonymous", restaurantId);
            throw new HubException("Forbidden");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, Group(restaurantId));
        _logger.LogInformation("Subscribed connection {ConnectionId} to {Group}", Context.ConnectionId, Group(restaurantId));
    }

    public async Task UnsubscribeFromRestaurant(Guid restaurantId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(restaurantId));
        _logger.LogInformation("Unsubscribed connection {ConnectionId} from {Group}", Context.ConnectionId, Group(restaurantId));
    }
}
