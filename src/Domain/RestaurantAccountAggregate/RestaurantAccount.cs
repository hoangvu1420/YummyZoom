using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.Errors;
using YummyZoom.Domain.RestaurantAccountAggregate.Events;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.Common.Models;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAccountAggregate;

public sealed class RestaurantAccount : AggregateRoot<RestaurantAccountId, Guid>, ICreationAuditable
{
    public RestaurantId RestaurantId { get; private set; }
    public Money CurrentBalance { get; private set; } 
    public PayoutMethodDetails? PayoutMethodDetails { get; private set; }

    // Creation audit properties (immutable aggregate)
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }

    private RestaurantAccount(
        RestaurantAccountId id,
        RestaurantId restaurantId,
        Money currentBalance,
        PayoutMethodDetails? payoutMethodDetails)
        : base(id)
    {
        RestaurantId = restaurantId;
        CurrentBalance = currentBalance;
        PayoutMethodDetails = payoutMethodDetails;
    }

    public static Result<RestaurantAccount> Create(RestaurantId restaurantId)
    {
        var zeroBalance = new Money(0, Currencies.Default);
        
        var account = new RestaurantAccount(
            RestaurantAccountId.CreateUnique(),
            restaurantId,
            zeroBalance,
            payoutMethodDetails: null);

        account.AddDomainEvent(new RestaurantAccountCreated(
            (RestaurantAccountId)account.Id, 
            restaurantId));

        return Result.Success(account);
    }

    public Result RecordRevenue(Money amount, OrderId orderId)
    {
        if (amount.Amount <= 0)
        {
            return Result.Failure(RestaurantAccountErrors.OrderRevenueMustBePositive(amount));
        }
        CurrentBalance += amount;
        AddDomainEvent(new RevenueRecorded((RestaurantAccountId)Id, orderId, amount));
        return Result.Success();
    }

    public Result RecordPlatformFee(Money feeAmount, OrderId orderId)
    {
        if (feeAmount.Amount >= 0)
        {
            return Result.Failure(RestaurantAccountErrors.PlatformFeeMustBeNegative(feeAmount));
        }
        CurrentBalance += feeAmount;
        AddDomainEvent(new PlatformFeeRecorded((RestaurantAccountId)Id, orderId, feeAmount));
        return Result.Success();
    }
    
    public Result RecordRefundDeduction(Money refundAmount, OrderId orderId)
    {
        if (refundAmount.Amount >= 0)
        {
            return Result.Failure(RestaurantAccountErrors.RefundDeductionMustBeNegative(refundAmount));
        }
        CurrentBalance += refundAmount;
        AddDomainEvent(new RefundDeducted((RestaurantAccountId)Id, orderId, refundAmount));
        return Result.Success();
    }
    
    public Result SettlePayout(Money payoutAmount)
    {
        if (payoutAmount.Amount <= 0)
        {
             return Result.Failure(RestaurantAccountErrors.PayoutAmountMustBePositive(payoutAmount));
        }
        
        if (payoutAmount.Amount > CurrentBalance.Amount)
        {
            return Result.Failure(RestaurantAccountErrors.InsufficientBalance(CurrentBalance, payoutAmount));
        }

        CurrentBalance -= payoutAmount;
        AddDomainEvent(new PayoutSettled((RestaurantAccountId)Id, payoutAmount, CurrentBalance));
        return Result.Success();
    }

    public Result MakeManualAdjustment(Money adjustmentAmount, string reason, Guid adminId)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(RestaurantAccountErrors.ManualAdjustmentReasonRequired);
        }

        CurrentBalance += adjustmentAmount;
        AddDomainEvent(new ManualAdjustmentMade((RestaurantAccountId)Id, adjustmentAmount, reason, adminId));
        return Result.Success();
    }

    public Result UpdatePayoutMethod(PayoutMethodDetails payoutMethodDetails)
    {
        PayoutMethodDetails = payoutMethodDetails;
        AddDomainEvent(new PayoutMethodUpdated((RestaurantAccountId)Id, payoutMethodDetails));
        return Result.Success();
    }

    /// <summary>
    /// Marks this restaurant account as deleted. This is the single, authoritative way to delete this aggregate.
    /// </summary>
    /// <returns>A Result indicating success</returns>
    public Result MarkAsDeleted()
    {
        AddDomainEvent(new RestaurantAccountDeleted((RestaurantAccountId)Id));

        return Result.Success();
    }

#pragma warning disable CS8618
    private RestaurantAccount() { }
#pragma warning restore CS8618
}
