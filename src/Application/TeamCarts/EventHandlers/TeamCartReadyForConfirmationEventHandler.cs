using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

public sealed class TeamCartReadyForConfirmationEventHandler : IdempotentNotificationHandler<TeamCartReadyForConfirmation>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ILogger<TeamCartReadyForConfirmationEventHandler> _logger;

    public TeamCartReadyForConfirmationEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRealtimeNotifier notifier,
        ILogger<TeamCartReadyForConfirmationEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(TeamCartReadyForConfirmation notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogInformation("Handling TeamCartReadyForConfirmation (EventId={EventId}, CartId={CartId})",
            notification.EventId, cartId.Value);

        try
        {
            // If VM maintains a separate flag, a store method could be added later. For now, just broadcast.
            await _notifier.NotifyReadyToConfirm(cartId, ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle TeamCartReadyForConfirmation (CartId={CartId}, EventId={EventId})",
                cartId.Value, notification.EventId);
            throw;
        }
    }
}
