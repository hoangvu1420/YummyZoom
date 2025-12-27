using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.Errors;
using YummyZoom.Domain.RestaurantAccountAggregate.Events;
using YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAccountAggregate;

public sealed class RestaurantAccount : AggregateRoot<RestaurantAccountId, Guid>, ICreationAuditable
{
    public RestaurantId RestaurantId { get; private set; }
    public Money CurrentBalance { get; private set; }
    public Money PendingPayoutTotal { get; private set; }
    public PayoutMethodDetails? PayoutMethodDetails { get; private set; }

    // Creation audit properties (immutable aggregate)
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }

    private RestaurantAccount(
        RestaurantAccountId id,
        RestaurantId restaurantId,
        Money currentBalance,
        Money pendingPayoutTotal,
        PayoutMethodDetails? payoutMethodDetails)
        : base(id)
    {
        RestaurantId = restaurantId;
        CurrentBalance = currentBalance;
        PendingPayoutTotal = pendingPayoutTotal;
        PayoutMethodDetails = payoutMethodDetails;
    }

    public static Result<RestaurantAccount> Create(RestaurantId restaurantId)
    {
        var zeroBalance = new Money(0, Currencies.Default);
        var zeroPendingPayout = new Money(0, Currencies.Default);

        var account = new RestaurantAccount(
            RestaurantAccountId.CreateUnique(),
            restaurantId,
            zeroBalance,
            zeroPendingPayout,
            payoutMethodDetails: null);

        account.AddDomainEvent(new RestaurantAccountCreated(
            account.Id,
            restaurantId));

        return Result.Success(account);
    }

    public Money GetAvailableBalance()
    {
        return CurrentBalance - PendingPayoutTotal;
    }

    public Result ReservePayout(Money holdAmount)
    {
        if (holdAmount.Amount <= 0)
        {
            return Result.Failure(RestaurantAccountErrors.PayoutHoldMustBePositive(holdAmount));
        }

        if (holdAmount.Currency != CurrentBalance.Currency)
        {
            return Result.Failure(RestaurantAccountErrors.PayoutCurrencyMismatch(CurrentBalance.Currency, holdAmount.Currency));
        }

        var availableBalance = GetAvailableBalance();
        if (holdAmount.Amount > availableBalance.Amount)
        {
            return Result.Failure(RestaurantAccountErrors.InsufficientAvailableBalance(availableBalance, holdAmount));
        }

        PendingPayoutTotal += holdAmount;
        return Result.Success();
    }

    public Result ReleasePayoutHold(Money releaseAmount)
    {
        if (releaseAmount.Amount <= 0)
        {
            return Result.Failure(RestaurantAccountErrors.PayoutHoldMustBePositive(releaseAmount));
        }

        if (releaseAmount.Currency != PendingPayoutTotal.Currency)
        {
            return Result.Failure(RestaurantAccountErrors.PayoutCurrencyMismatch(PendingPayoutTotal.Currency, releaseAmount.Currency));
        }

        if (releaseAmount.Amount > PendingPayoutTotal.Amount)
        {
            return Result.Failure(RestaurantAccountErrors.InsufficientPayoutHold(PendingPayoutTotal, releaseAmount));
        }

        PendingPayoutTotal -= releaseAmount;
        return Result.Success();
    }

    public Result RecordRevenue(Money amount, OrderId orderId)
    {
        if (amount.Amount <= 0)
        {
            return Result.Failure(RestaurantAccountErrors.OrderRevenueMustBePositive(amount));
        }
        CurrentBalance += amount;
        AddDomainEvent(new RevenueRecorded(Id, orderId, amount));
        return Result.Success();
    }

    public Result RecordPlatformFee(Money feeAmount, OrderId orderId)
    {
        if (feeAmount.Amount >= 0)
        {
            return Result.Failure(RestaurantAccountErrors.PlatformFeeMustBeNegative(feeAmount));
        }
        CurrentBalance += feeAmount;
        AddDomainEvent(new PlatformFeeRecorded(Id, orderId, feeAmount));
        return Result.Success();
    }

    public Result RecordRefundDeduction(Money refundAmount, OrderId orderId)
    {
        if (refundAmount.Amount >= 0)
        {
            return Result.Failure(RestaurantAccountErrors.RefundDeductionMustBeNegative(refundAmount));
        }
        CurrentBalance += refundAmount;
        AddDomainEvent(new RefundDeducted(Id, orderId, refundAmount));
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
        AddDomainEvent(new PayoutSettled(Id, payoutAmount, CurrentBalance));
        return Result.Success();
    }

    public Result MakeManualAdjustment(Money adjustmentAmount, string reason, Guid adminId)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(RestaurantAccountErrors.ManualAdjustmentReasonRequired);
        }

        CurrentBalance += adjustmentAmount;
        AddDomainEvent(new ManualAdjustmentMade(Id, adjustmentAmount, reason, adminId));
        return Result.Success();
    }

    public Result UpdatePayoutMethod(PayoutMethodDetails payoutMethodDetails)
    {
        PayoutMethodDetails = payoutMethodDetails;
        AddDomainEvent(new PayoutMethodUpdated(Id, payoutMethodDetails));
        return Result.Success();
    }

    /// <summary>
    /// Marks this restaurant account as deleted. This is the single, authoritative way to delete this aggregate.
    /// </summary>
    /// <returns>A Result indicating success</returns>
    public Result MarkAsDeleted()
    {
        AddDomainEvent(new RestaurantAccountDeleted(Id));

        return Result.Success();
    }

#pragma warning disable CS8618
    private RestaurantAccount() { }
#pragma warning restore CS8618
}
