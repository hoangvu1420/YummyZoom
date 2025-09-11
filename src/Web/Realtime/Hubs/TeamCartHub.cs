using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Web.Realtime.Hubs;

/// <summary>
/// Hub for TeamCart collaboration updates. Behind feature flag.
/// Authorization: requires authenticated user; membership enforcement can be added in subsequent phases.
/// </summary>
[Authorize]
public sealed class TeamCartHub : Hub
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<TeamCartHub> _logger;

    public TeamCartHub(
        IAuthorizationService authorizationService,
        ILogger<TeamCartHub> logger)
    {
        _authorizationService = authorizationService;
        _logger = logger;
    }

    private static string Group(Guid cartId) => $"teamcart:{cartId}";

    private sealed record TeamCartResource(Guid Id) : ITeamCartQuery
    {
        public TeamCartId TeamCartId => TeamCartId.Create(Id);
    }

    public async Task SubscribeToCart(Guid cartId)
    {
        var user = Context.User;
        if (user is null)
        {
            throw new HubException("Unauthorized");
        }

        // Enforce membership via policy-based authorization (Application-layer patterns)
        var resource = new TeamCartResource(cartId);
        var authz = await _authorizationService.AuthorizeAsync(user, resource, Policies.MustBeTeamCartMember);
        if (!authz.Succeeded)
        {
            _logger.LogWarning("Hub Subscribe forbidden: UserId={UserId}, TeamCartId={TeamCartId}",
                user.Identity?.Name ?? "anonymous", cartId);
            throw new HubException("Forbidden");
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
