using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Handles TeamCartLockedForPayment by setting the VM status to Locked and notifying clients.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class TeamCartLockedForPaymentEventHandler : IdempotentNotificationHandler<TeamCartLockedForPayment>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ITeamCartPushNotifier _pushNotifier;
    private readonly ILogger<TeamCartLockedForPaymentEventHandler> _logger;

    public TeamCartLockedForPaymentEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRepository teamCartRepository,
        ITeamCartRealtimeNotifier notifier,
        ITeamCartPushNotifier pushNotifier,
        ILogger<TeamCartLockedForPaymentEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _teamCartRepository = teamCartRepository;
        _notifier = notifier;
        _pushNotifier = pushNotifier;
        _logger = logger;
    }

    protected override async Task HandleCore(TeamCartLockedForPayment notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogDebug("Handling TeamCartLockedForPayment (EventId={EventId}, CartId={CartId}, HostUserId={HostUserId})",
            notification.EventId, cartId.Value, notification.HostUserId.Value);

        var cart = await _teamCartRepository.GetByIdAsync(cartId, ct);
        if (cart is null)
        {
            _logger.LogWarning("TeamCartLockedForPayment handler could not find cart (CartId={CartId}, EventId={EventId})", cartId.Value, notification.EventId);
            return;
        }

        try
        {
            await _store.SetLockedAsync(cartId, ct);
            await _notifier.NotifyLocked(cartId, ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
            
            var vm = await _store.GetVmAsync(cartId, ct);
            if (vm is not null)
            {
                // Notify members (host already knows they locked it)
                var hostMember = vm.Members.FirstOrDefault(m => m.Role == "Host");
                var context = new TeamCartNotificationContext
                {
                    EventType = "TeamCartLockedForPayment",
                    ActorUserId = notification.HostUserId.Value,
                    ActorName = hostMember?.Name ?? "Chủ giỏ"
                };
                
                var push = await _pushNotifier.PushTeamCartDataAsync(
                    cartId, 
                    vm.Version, 
                    TeamCartNotificationTarget.Members,
                    context,
                    NotificationDeliveryType.Hybrid,
                    ct);
                if (push.IsFailure)
                {
                    throw new InvalidOperationException(push.Error.Description);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set locked in VM or notify (CartId={CartId}, EventId={EventId})",
                cartId.Value, notification.EventId);
            throw; // allow outbox retry
        }
    }
}

