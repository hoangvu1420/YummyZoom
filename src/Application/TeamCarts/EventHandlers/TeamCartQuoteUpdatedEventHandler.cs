using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Projects TeamCartQuoteUpdated into the Redis VM with idempotency and version guarding.
/// </summary>
public sealed class TeamCartQuoteUpdatedEventHandler : IdempotentNotificationHandler<TeamCartQuoteUpdated>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ILogger<TeamCartQuoteUpdatedEventHandler> _logger;

    public TeamCartQuoteUpdatedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ITeamCartStore store,
        ITeamCartRealtimeNotifier notifier,
        ILogger<TeamCartQuoteUpdatedEventHandler> logger) : base(uow, inbox)
    {
        _store = store;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(TeamCartQuoteUpdated notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogInformation("Handling TeamCartQuoteUpdated (EventId={EventId}, CartId={CartId}, Version={Version})",
            notification.EventId, cartId.Value, notification.QuoteVersion);

        try
        {
            var vm = await _store.GetVmAsync(cartId, ct);
            var currentVersion = vm?.QuoteVersion ?? 0L;

            if (currentVersion >= notification.QuoteVersion)
            {
                _logger.LogInformation("Skipping TeamCartQuoteUpdated due to stale/non-advancing version (Current={Current}, Incoming={Incoming}, CartId={CartId})",
                    currentVersion, notification.QuoteVersion, cartId.Value);
                return;
            }

            await _store.UpdateQuoteAsync(cartId, notification.QuoteVersion, notification.MemberQuotedAmounts, notification.Currency, ct);
            await _notifier.NotifyCartUpdated(cartId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update quote in VM or notify (CartId={CartId}, Version={Version}, EventId={EventId})",
                cartId.Value, notification.QuoteVersion, notification.EventId);
            throw; // allow outbox retry
        }

        _logger.LogInformation("Handled TeamCartQuoteUpdated (EventId={EventId}, CartId={CartId}, Version={Version})",
            notification.EventId, cartId.Value, notification.QuoteVersion);
    }
}
