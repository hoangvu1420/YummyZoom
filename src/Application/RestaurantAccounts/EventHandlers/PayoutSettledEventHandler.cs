using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.AccountTransactionEntity;
using YummyZoom.Domain.AccountTransactionEntity.Enums;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.Events;

namespace YummyZoom.Application.RestaurantAccounts.EventHandlers;

public sealed class PayoutSettledEventHandler : IdempotentNotificationHandler<PayoutSettled>
{
    private readonly IAccountTransactionRepository _accountTransactionRepository;
    private readonly ILogger<PayoutSettledEventHandler> _logger;

    public PayoutSettledEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IAccountTransactionRepository accountTransactionRepository,
        ILogger<PayoutSettledEventHandler> logger) : base(uow, inbox)
    {
        _accountTransactionRepository = accountTransactionRepository;
        _logger = logger;
    }

    protected override async Task HandleCore(PayoutSettled notification, CancellationToken ct)
    {
        _logger.LogDebug(
            "Handling PayoutSettled (EventId={EventId}, AccountId={AccountId})",
            notification.EventId,
            notification.RestaurantAccountId.Value);

        var payoutAmount = notification.PayoutAmount;
        var ledgerAmount = new Money(-payoutAmount.Amount, payoutAmount.Currency);

        var transactionResult = AccountTransaction.Create(
            notification.RestaurantAccountId,
            TransactionType.PayoutSettlement,
            ledgerAmount);

        if (transactionResult.IsFailure)
        {
            _logger.LogError(
                "Failed to create payout settlement transaction (AccountId={AccountId}, Error={Error})",
                notification.RestaurantAccountId.Value,
                transactionResult.Error);
            throw new InvalidOperationException(transactionResult.Error.Description);
        }

        await _accountTransactionRepository.AddAsync(transactionResult.Value, ct);
    }
}
