using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.Entities;
using YummyZoom.Domain.RestaurantAccountAggregate.Enums;
using YummyZoom.Domain.RestaurantAccountAggregate.Errors;
using YummyZoom.Domain.RestaurantAccountAggregate.Events;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAccountAggregate;

public sealed class RestaurantAccount : AggregateRoot<RestaurantAccountId, Guid>
{
    private readonly List<AccountTransaction> _transactions = [];

    public RestaurantId RestaurantId { get; private set; }
    public Money CurrentBalance { get; private set; }
    public PayoutMethodDetails? PayoutMethodDetails { get; private set; }

    public IReadOnlyList<AccountTransaction> Transactions => _transactions.AsReadOnly();

    private RestaurantAccount(
        RestaurantAccountId id,
        RestaurantId restaurantId,
        Money currentBalance,
        PayoutMethodDetails? payoutMethodDetails,
        List<AccountTransaction> transactions)  
        : base(id)
    {
        RestaurantId = restaurantId;
        CurrentBalance = currentBalance;
        PayoutMethodDetails = payoutMethodDetails;
        _transactions = transactions;
    }

    public static RestaurantAccount Create(RestaurantId restaurantId)
    {
        var zeroBalance = Money.Create(0).Value; // Should always succeed for 0
        
        var account = new RestaurantAccount(
            RestaurantAccountId.CreateUnique(),
            restaurantId,
            zeroBalance,
            payoutMethodDetails: null,
            transactions: []);

        account.AddDomainEvent(new RestaurantAccountCreated(
            (RestaurantAccountId)account.Id, 
            restaurantId));

        return account;
    }

    public Result AddOrderRevenue(Money amount, OrderId orderId)
    {
        var transactionResult = AccountTransaction.Create(
            TransactionType.OrderRevenue, 
            amount, 
            orderId);

        if (transactionResult.IsFailure)
        {
            return Result.Failure(transactionResult.Error);
        }

        return AddTransaction(transactionResult.Value);
    }

    public Result AddPlatformFee(Money feeAmount, OrderId orderId)
    {
        var transactionResult = AccountTransaction.Create(
            TransactionType.PlatformFee, 
            feeAmount, 
            orderId);

        if (transactionResult.IsFailure)
        {
            return Result.Failure(transactionResult.Error);
        }

        return AddTransaction(transactionResult.Value);
    }

    public Result AddRefundDeduction(Money refundAmount, OrderId orderId)
    {
        var transactionResult = AccountTransaction.Create(
            TransactionType.RefundDeduction, 
            refundAmount, 
            orderId);

        if (transactionResult.IsFailure)
        {
            return Result.Failure(transactionResult.Error);
        }

        return AddTransaction(transactionResult.Value);
    }

    public Result ProcessPayout(Money payoutAmount)
    {
        // Validate payout amount is not greater than current balance
        if (payoutAmount.Amount > CurrentBalance.Amount)
        {
            return Result.Failure(RestaurantAccountErrors.InsufficientBalance);
        }

        // Create negative amount for payout (debit transaction)
        var negativePayoutAmount = Money.Create(-payoutAmount.Amount, payoutAmount.Currency).Value;
        
        var transactionResult = AccountTransaction.Create(
            TransactionType.PayoutSettlement, 
            negativePayoutAmount);

        if (transactionResult.IsFailure)
        {
            return Result.Failure(transactionResult.Error);
        }

        var result = AddTransaction(transactionResult.Value);
        if (result.IsFailure)
        {
            return result;
        }

        AddDomainEvent(new PayoutSettled(
            (RestaurantAccountId)Id, 
            payoutAmount, 
            CurrentBalance));

        return Result.Success();
    }

    public Result AddManualAdjustment(Money adjustmentAmount)
    {
        var transactionResult = AccountTransaction.Create(
            TransactionType.ManualAdjustment, 
            adjustmentAmount);

        if (transactionResult.IsFailure)
        {
            return Result.Failure(transactionResult.Error);
        }

        return AddTransaction(transactionResult.Value);
    }

    public Result UpdatePayoutMethod(PayoutMethodDetails payoutMethodDetails)
    {
        PayoutMethodDetails = payoutMethodDetails;
        
        AddDomainEvent(new PayoutMethodUpdated(
            (RestaurantAccountId)Id, 
            payoutMethodDetails));

        return Result.Success();
    }

    private Result AddTransaction(AccountTransaction transaction)
    {
        _transactions.Add(transaction);
        
        // Recalculate balance and verify invariant
        var newBalance = Money.Create(
            _transactions.Sum(t => t.Amount.Amount), 
            CurrentBalance.Currency).Value;
        
        CurrentBalance = newBalance;
        
        // Verify the critical invariant
        if (!ValidateBalanceConsistency())
        {
            return Result.Failure(RestaurantAccountErrors.BalanceInconsistency);
        }

        AddDomainEvent(new TransactionAdded(
            (RestaurantAccountId)Id,
            transaction.Id,
            transaction.Type,
            transaction.Amount,
            transaction.RelatedOrderId));

        return Result.Success();
    }

    private bool ValidateBalanceConsistency()
    {
        var calculatedBalance = _transactions.Sum(t => t.Amount.Amount);
        return Math.Abs(calculatedBalance - CurrentBalance.Amount) < 0.01m; // Account for floating point precision
    }

#pragma warning disable CS8618
    private RestaurantAccount() { }
#pragma warning restore CS8618
}
