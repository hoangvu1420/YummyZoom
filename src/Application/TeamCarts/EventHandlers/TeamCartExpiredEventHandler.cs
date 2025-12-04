using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Handles TeamCartExpired by deleting the Redis VM and notifying clients.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class TeamCartExpiredEventHandler : IdempotentNotificationHandler<TeamCartExpired>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ITeamCartPushNotifier _pushNotifier;
    private readonly ILogger<TeamCartExpiredEventHandler> _logger;

    public TeamCartExpiredEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRepository teamCartRepository,
        ITeamCartRealtimeNotifier notifier,
        ITeamCartPushNotifier pushNotifier,
        ILogger<TeamCartExpiredEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _teamCartRepository = teamCartRepository;
        _notifier = notifier;
        _pushNotifier = pushNotifier;
        _logger = logger;
    }

    protected override async Task HandleCore(TeamCartExpired notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogDebug("Handling TeamCartExpired (EventId={EventId}, CartId={CartId})",
            notification.EventId, cartId.Value);

        try
        {
            // Get version before deletion
            var vm = await _store.GetVmAsync(cartId, ct);
            var version = vm?.Version ?? 0;
            
            await _store.DeleteVmAsync(cartId, ct);
            await _notifier.NotifyExpired(cartId, ct);
            
            // Push notification with version from VM (before deletion)
            if (version > 0)
            {
                var push = await _pushNotifier.PushTeamCartDataAsync(cartId, version, ct);
                if (push.IsFailure)
                {
                    throw new InvalidOperationException(push.Error.Description);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete VM or notify expired (CartId={CartId}, EventId={EventId})",
                cartId.Value, notification.EventId);
            throw; // allow outbox retry
        }
    }
}


