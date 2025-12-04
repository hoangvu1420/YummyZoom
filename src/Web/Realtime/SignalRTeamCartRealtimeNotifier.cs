using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Web.Realtime.Hubs;

namespace YummyZoom.Web.Realtime;

/// <summary>
/// SignalR-backed implementation of ITeamCartRealtimeNotifier.
/// Sends messages to clients subscribed to the TeamCart group.
/// FCM push notifications are handled explicitly in event handlers.
/// </summary>
public sealed class SignalRTeamCartRealtimeNotifier : ITeamCartRealtimeNotifier
{
    private readonly IHubContext<TeamCartHub> _hubContext;
    private readonly ILogger<SignalRTeamCartRealtimeNotifier> _logger;

    public SignalRTeamCartRealtimeNotifier(
        IHubContext<TeamCartHub> hubContext,
        ILogger<SignalRTeamCartRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    private static string Group(TeamCartId id) => $"teamcart:{id.Value}";

    public Task NotifyCartUpdated(TeamCartId cartId, CancellationToken cancellationToken = default)
        => SendAsync(cartId, "ReceiveCartUpdated", cancellationToken);

    public Task NotifyLocked(TeamCartId cartId, CancellationToken cancellationToken = default)
        => SendAsync(cartId, "ReceiveLocked", cancellationToken);

    public Task NotifyPaymentEvent(TeamCartId cartId, Guid userId, string status, CancellationToken cancellationToken = default)
        => SendAsync(cartId, "ReceivePaymentEvent", cancellationToken, userId, status);

    public Task NotifyReadyToConfirm(TeamCartId cartId, CancellationToken cancellationToken = default)
        => SendAsync(cartId, "ReceiveReadyToConfirm", cancellationToken);

    public Task NotifyConverted(TeamCartId cartId, Guid orderId, CancellationToken cancellationToken = default)
        => SendAsync(cartId, "ReceiveConverted", cancellationToken, orderId);

    public Task NotifyAllMembersReady(TeamCartId cartId, CancellationToken cancellationToken = default)
        => SendAsync(cartId, "ReceiveAllMembersReady", cancellationToken);

    public Task NotifyExpired(TeamCartId cartId, CancellationToken cancellationToken = default)
        => SendAsync(cartId, "ReceiveExpired", cancellationToken);

    private async Task SendAsync(TeamCartId cartId, string method, CancellationToken ct, params object?[] args)
    {
        try
        {
            await _hubContext.Clients.Group(Group(cartId)).SendAsync(method, args, ct);
            _logger.LogDebug("Broadcasted {Method} for TeamCartId={CartId} to group {Group}", method, cartId.Value, Group(cartId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast {Method} for TeamCartId={CartId} to group {Group}", method, cartId.Value, Group(cartId));
            throw; // Re-throw to allow handler retry
        }
    }
}
