using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

public sealed class TeamCartConvertedEventHandler : IdempotentNotificationHandler<TeamCartConverted>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ITeamCartPushNotifier _pushNotifier;
    private readonly ILogger<TeamCartConvertedEventHandler> _logger;

    public TeamCartConvertedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRepository teamCartRepository,
        ITeamCartRealtimeNotifier notifier,
        ITeamCartPushNotifier pushNotifier,
        ILogger<TeamCartConvertedEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _teamCartRepository = teamCartRepository;
        _notifier = notifier;
        _pushNotifier = pushNotifier;
        _logger = logger;
    }

    protected override async Task HandleCore(TeamCartConverted notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogDebug("Handling TeamCartConverted (EventId={EventId}, CartId={CartId}, OrderId={OrderId})",
            notification.EventId, cartId.Value, notification.OrderId.Value);

        try
        {
            // Get version before deletion
            var vm = await _store.GetVmAsync(cartId, ct);
            var version = vm?.Version ?? 0;
            
            await _store.DeleteVmAsync(cartId, ct);
            await _notifier.NotifyConverted(cartId, notification.OrderId.Value, ct);
            
            // Push notification with version from VM (before deletion)
            if (version > 0)
            {
                var context = new TeamCartNotificationContext
                {
                    EventType = "TeamCartConverted",
                    OrderId = notification.OrderId.Value
                };
                
                // Notify all members
                var push = await _pushNotifier.PushTeamCartDataAsync(
                    cartId, 
                    version, 
                    TeamCartNotificationTarget.All,
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
            _logger.LogError(ex, "Failed to handle TeamCartConverted (CartId={CartId}, EventId={EventId})",
                cartId.Value, notification.EventId);
            throw;
        }
    }
}


