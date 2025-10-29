using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Web.Realtime.Hubs;

namespace YummyZoom.Web.Realtime;

/// <summary>
/// SignalR-backed implementation of ITeamCartRealtimeNotifier.
/// Sends messages to clients subscribed to the TeamCart group.
/// </summary>
public sealed class SignalRTeamCartRealtimeNotifier : ITeamCartRealtimeNotifier
{
    private readonly IHubContext<TeamCartHub> _hubContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SignalRTeamCartRealtimeNotifier> _logger;

    public SignalRTeamCartRealtimeNotifier(
        IHubContext<TeamCartHub> hubContext,
        IServiceProvider serviceProvider,
        ILogger<SignalRTeamCartRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _serviceProvider = serviceProvider;
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

    public Task NotifyExpired(TeamCartId cartId, CancellationToken cancellationToken = default)
        => SendAsync(cartId, "ReceiveExpired", cancellationToken);

    private async Task SendAsync(TeamCartId cartId, string method, CancellationToken ct, params object?[] args)
    {
        try
        {
            await _hubContext.Clients.Group(Group(cartId)).SendAsync(method, args, ct);
            _logger.LogDebug("Broadcasted {Method} for TeamCartId={CartId} to group {Group}", method, cartId.Value, Group(cartId));

            // Also emit FCM data-only push to all active member devices (scoped dependency)
            using var scope = _serviceProvider.CreateScope();
            var push = scope.ServiceProvider.GetRequiredService<ITeamCartPushNotifier>();
            var pushResult = await push.PushTeamCartDataAsync(cartId, ct);
            if (pushResult.IsFailure)
            {
                _logger.LogWarning("Failed to send TeamCart FCM data push (CartId={CartId}): {Error}", cartId.Value, pushResult.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast {Method} for TeamCartId={CartId} to group {Group}", method, cartId.Value, Group(cartId));
        }
    }
}
