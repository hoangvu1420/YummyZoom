using YummyZoom.Domain.AccountTransactionEntity.Enums;
using YummyZoom.Domain.AccountTransactionEntity.ValueObjects;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.Errors;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.AccountTransactionEntity;

public sealed class AccountTransaction : Entity<AccountTransactionId>
{
    public RestaurantAccountId RestaurantAccountId { get; private set; }
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; }
    public DateTime Timestamp { get; private set; }
    public OrderId? RelatedOrderId { get; private set; }
    public string? Notes { get; private set; }

    private AccountTransaction(
        AccountTransactionId id,
        RestaurantAccountId restaurantAccountId,
        TransactionType type,
        Money amount,
        DateTime timestamp,
        OrderId? relatedOrderId,
        string? notes)
        : base(id)
    {
        RestaurantAccountId = restaurantAccountId;
        Type = type;
        Amount = amount;
        Timestamp = timestamp;
        RelatedOrderId = relatedOrderId;
        Notes = notes;
    }

    public static Result<AccountTransaction> Create(
        RestaurantAccountId restaurantAccountId,
        TransactionType type,
        Money amount,
        OrderId? relatedOrderId = null,
        string? notes = null)
    {
        var validationResult = ValidateTransaction(type, amount);
        if (validationResult.IsFailure)
        {
            return Result.Failure<AccountTransaction>(validationResult.Error);
        }

        return Result.Success(new AccountTransaction(
            AccountTransactionId.CreateUnique(),
            restaurantAccountId,
            type,
            amount,
            DateTime.UtcNow,
            relatedOrderId,
            notes));
    }
    
    private static Result ValidateTransaction(TransactionType type, Money amount)
    { 
        // Using the same errors from RestaurantAccountErrors for consistency.
        return type switch
        {
            TransactionType.OrderRevenue when amount.Amount <= 0 => 
                Result.Failure(RestaurantAccountErrors.OrderRevenueMustBePositive(amount)),
            TransactionType.PlatformFee when amount.Amount >= 0 => 
                Result.Failure(RestaurantAccountErrors.PlatformFeeMustBeNegative(amount)),
            TransactionType.RefundDeduction when amount.Amount >= 0 =>
                Result.Failure(RestaurantAccountErrors.RefundDeductionMustBeNegative(amount)),
            _ => Result.Success()
        };
    }

#pragma warning disable CS8618
    private AccountTransaction() { }
#pragma warning restore CS8618
}
