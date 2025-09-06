using YummyZoom.Domain.TeamCartAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces.IServices;

/// <summary>
/// Abstraction for pushing real-time TeamCart updates to connected clients.
/// Default implementation is No-Op in Infrastructure; Web host overrides with SignalR-backed adapter.
/// </summary>
public interface ITeamCartRealtimeNotifier
{
    Task NotifyCartUpdated(TeamCartId cartId, CancellationToken cancellationToken = default);
    Task NotifyLocked(TeamCartId cartId, CancellationToken cancellationToken = default);
    Task NotifyPaymentEvent(TeamCartId cartId, Guid userId, string status, CancellationToken cancellationToken = default);
    Task NotifyReadyToConfirm(TeamCartId cartId, CancellationToken cancellationToken = default);
    Task NotifyConverted(TeamCartId cartId, Guid orderId, CancellationToken cancellationToken = default);
    Task NotifyExpired(TeamCartId cartId, CancellationToken cancellationToken = default);
}

