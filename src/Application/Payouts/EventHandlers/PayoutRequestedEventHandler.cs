using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.PayoutAggregate.Events;

namespace YummyZoom.Application.Payouts.EventHandlers;

public sealed class PayoutRequestedEventHandler : IdempotentNotificationHandler<PayoutRequested>
{
    private readonly IPayoutRepository _payoutRepository;
    private readonly IPayoutProvider _payoutProvider;
    private readonly ILogger<PayoutRequestedEventHandler> _logger;

    public PayoutRequestedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IPayoutRepository payoutRepository,
        IPayoutProvider payoutProvider,
        ILogger<PayoutRequestedEventHandler> logger) : base(uow, inbox)
    {
        _payoutRepository = payoutRepository;
        _payoutProvider = payoutProvider;
        _logger = logger;
    }

    protected override async Task HandleCore(PayoutRequested notification, CancellationToken ct)
    {
        _logger.LogDebug("Handling PayoutRequested (EventId={EventId}, PayoutId={PayoutId})",
            notification.EventId, notification.PayoutId.Value);

        var payout = await _payoutRepository.GetByIdAsync(notification.PayoutId, ct);
        if (payout is null)
        {
            _logger.LogWarning("PayoutRequested handler could not find payout (PayoutId={PayoutId}, EventId={EventId})",
                notification.PayoutId.Value, notification.EventId);
            return;
        }

        var providerResult = await _payoutProvider.RequestPayoutAsync(
            new PayoutProviderRequest(
                payout.Id.Value,
                payout.Amount.Amount,
                payout.Amount.Currency,
                payout.IdempotencyKey),
            ct);

        if (providerResult.IsFailure)
        {
            _logger.LogError(
                "Payout provider request failed (PayoutId={PayoutId}, Error={Error})",
                payout.Id.Value,
                providerResult.Error);
            throw new InvalidOperationException(providerResult.Error.Description);
        }

        var processingResult = payout.MarkProcessing(providerResult.Value.ProviderReferenceId);
        if (processingResult.IsFailure)
        {
            _logger.LogError(
                "Failed to mark payout processing (PayoutId={PayoutId}, Error={Error})",
                payout.Id.Value,
                processingResult.Error);
            throw new InvalidOperationException(processingResult.Error.Description);
        }

        await _payoutRepository.UpdateAsync(payout, ct);
    }
}
