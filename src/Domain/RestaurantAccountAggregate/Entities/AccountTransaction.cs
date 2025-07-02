using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.Enums;
using YummyZoom.Domain.RestaurantAccountAggregate.Errors;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAccountAggregate.Entities;

public sealed class AccountTransaction : Entity<AccountTransactionId>
{
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; }
    public DateTime Timestamp { get; private set; }
    public OrderId? RelatedOrderId { get; private set; }

    private AccountTransaction(
        AccountTransactionId id,
        TransactionType type,
        Money amount,
        DateTime timestamp,
        OrderId? relatedOrderId)
        : base(id)
    {
        Type = type;
        Amount = amount;
        Timestamp = timestamp;
        RelatedOrderId = relatedOrderId;
    }

    public static Result<AccountTransaction> Create(
        TransactionType type,
        Money amount,
        OrderId? relatedOrderId = null)
    {
        // Validation based on business rules
        var validationResult = ValidateTransaction(type, amount);
        if (validationResult.IsFailure)
        {
            return Result.Failure<AccountTransaction>(validationResult.Error);
        }

        return Result.Success(new AccountTransaction(
            AccountTransactionId.CreateUnique(),
            type,
            amount,
            DateTime.UtcNow,
            relatedOrderId));
    }

    private static Result ValidateTransaction(TransactionType type, Money amount)
    {
        return type switch
        {
            TransactionType.OrderRevenue when amount.Amount <= 0 => 
                Result.Failure(RestaurantAccountErrors.OrderRevenueMustBePositive),
            TransactionType.PlatformFee when amount.Amount >= 0 => 
                Result.Failure(RestaurantAccountErrors.PlatformFeeMustBeNegative),
            TransactionType.RefundDeduction when amount.Amount >= 0 => 
                Result.Failure(RestaurantAccountErrors.RefundDeductionMustBeNegative),
            TransactionType.PayoutSettlement when amount.Amount >= 0 => 
                Result.Failure(RestaurantAccountErrors.PayoutSettlementMustBeNegative),
            _ => Result.Success()
        };
    }

#pragma warning disable CS8618
    private AccountTransaction() { }
#pragma warning restore CS8618
}
