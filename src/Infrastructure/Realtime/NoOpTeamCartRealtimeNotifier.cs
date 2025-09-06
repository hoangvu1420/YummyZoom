using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Infrastructure.Realtime;

/// <summary>
/// No-op notifier for TeamCart updates until SignalR hub is introduced and wired in the Web host.
/// </summary>
public sealed class NoOpTeamCartRealtimeNotifier : ITeamCartRealtimeNotifier
{
    public Task NotifyCartUpdated(TeamCartId cartId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyLocked(TeamCartId cartId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyPaymentEvent(TeamCartId cartId, Guid userId, string status, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyReadyToConfirm(TeamCartId cartId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyConverted(TeamCartId cartId, Guid orderId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyExpired(TeamCartId cartId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

