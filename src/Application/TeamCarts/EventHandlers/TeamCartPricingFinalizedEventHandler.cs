using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

public sealed class TeamCartPricingFinalizedEventHandler : IdempotentNotificationHandler<TeamCartPricingFinalized>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ITeamCartPushNotifier _pushNotifier;
    private readonly ILogger<TeamCartPricingFinalizedEventHandler> _logger;

    public TeamCartPricingFinalizedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRepository teamCartRepository,
        ITeamCartRealtimeNotifier notifier,
        ITeamCartPushNotifier pushNotifier,
        ILogger<TeamCartPricingFinalizedEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _teamCartRepository = teamCartRepository;
        _notifier = notifier;
        _pushNotifier = pushNotifier;
        _logger = logger;
    }

    protected override async Task HandleCore(TeamCartPricingFinalized notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        
        _logger.LogDebug("Handling TeamCartPricingFinalized (EventId={EventId}, CartId={CartId})",
            notification.EventId, cartId.Value);

        var cart = await _teamCartRepository.GetByIdAsync(cartId, ct);
        if (cart is null)
        {
            _logger.LogWarning("TeamCartPricingFinalized handler could not find cart (CartId={CartId}, EventId={EventId})", 
                cartId.Value, notification.EventId);
            return;
        }

        try
        {
            // Update Redis with the new status
            await _store.SetStatusAsync(cartId, TeamCartStatus.Finalized, ct);
            
            await _notifier.NotifyCartUpdated(cartId, ct);
            
            var vm = await _store.GetVmAsync(cartId, ct);
            if (vm is not null)
            {
                var context = new TeamCartNotificationContext
                {
                    EventType = "TeamCartPricingFinalized",
                    ActorUserId = notification.HostUserId.Value,
                    AdditionalInfo = "PricingFinalized"
                };
                
                // Notify all members (data-only, no notification tray)
                var push = await _pushNotifier.PushTeamCartDataAsync(
                    cartId, 
                    vm.Version, 
                    TeamCartNotificationTarget.All,
                    context,
                    NotificationDeliveryType.DataOnly,
                    ct);
                
                if (push.IsFailure)
                {
                    throw new InvalidOperationException(push.Error.Description);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle TeamCartPricingFinalized (CartId={CartId}, EventId={EventId})",
                cartId.Value, notification.EventId);
            throw;
        }
    }
}
