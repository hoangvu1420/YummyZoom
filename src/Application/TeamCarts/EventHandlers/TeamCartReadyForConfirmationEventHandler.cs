using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

public sealed class TeamCartReadyForConfirmationEventHandler : IdempotentNotificationHandler<TeamCartReadyForConfirmation>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ITeamCartPushNotifier _pushNotifier;
    private readonly ILogger<TeamCartReadyForConfirmationEventHandler> _logger;

    public TeamCartReadyForConfirmationEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRepository teamCartRepository,
        ITeamCartRealtimeNotifier notifier,
        ITeamCartPushNotifier pushNotifier,
        ILogger<TeamCartReadyForConfirmationEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _teamCartRepository = teamCartRepository;
        _notifier = notifier;
        _pushNotifier = pushNotifier;
        _logger = logger;
    }

    protected override async Task HandleCore(TeamCartReadyForConfirmation notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogDebug("Handling TeamCartReadyForConfirmation (EventId={EventId}, CartId={CartId})",
            notification.EventId, cartId.Value);

        var cart = await _teamCartRepository.GetByIdAsync(cartId, ct);
        if (cart is null)
        {
            _logger.LogWarning("TeamCartReadyForConfirmation handler could not find cart (CartId={CartId}, EventId={EventId})", cartId.Value, notification.EventId);
            return;
        }

        try
        {
            // If VM maintains a separate flag, a store method could be added later. For now, just broadcast.
            await _notifier.NotifyReadyToConfirm(cartId, ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
            
            var vm = await _store.GetVmAsync(cartId, ct);
            if (vm is not null)
            {
                var push = await _pushNotifier.PushTeamCartDataAsync(cartId, vm.Version, ct);
                if (push.IsFailure)
                {
                    throw new InvalidOperationException(push.Error.Description);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle TeamCartReadyForConfirmation (CartId={CartId}, EventId={EventId})",
                cartId.Value, notification.EventId);
            throw;
        }
    }
}
