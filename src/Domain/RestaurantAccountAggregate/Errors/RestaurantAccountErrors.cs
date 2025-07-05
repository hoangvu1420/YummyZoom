using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAccountAggregate.Errors;

public static class RestaurantAccountErrors
{
    public static Error InvalidId(string id) => Error.Validation(
        "RestaurantAccount.InvalidId", $"Restaurant account ID '{id}' is invalid.");

    public static readonly Error InvalidPayoutMethod = Error.Validation(
        "RestaurantAccount.InvalidPayoutMethod", "Payout method details cannot be empty.");

    public static Error OrderRevenueMustBePositive(Money amount) => Error.Validation(
        "RestaurantAccount.OrderRevenueMustBePositive", $"Order revenue '{amount}' must be a positive amount.");

    public static Error PlatformFeeMustBeNegative(Money amount) => Error.Validation(
        "RestaurantAccount.PlatformFeeMustBeNegative", $"Platform fee '{amount}' must be a negative amount.");

    public static Error RefundDeductionMustBeNegative(Money amount) => Error.Validation(
        "RestaurantAccount.RefundDeductionMustBeNegative", $"Refund deduction '{amount}' must be a negative amount.");

    public static Error PayoutAmountMustBePositive(Money amount) => Error.Validation(
        "RestaurantAccount.PayoutAmountMustBePositive", $"Payout settlement amount '{amount}' must be positive.");

    public static Error InsufficientBalance(Money currentBalance, Money payoutAmount) => Error.Validation(
        "RestaurantAccount.InsufficientBalance", $"Insufficient balance '{currentBalance}' for payout settlement of '{payoutAmount}'.");

    public static readonly Error ManualAdjustmentReasonRequired = Error.Validation(
        "RestaurantAccount.ManualAdjustmentReasonRequired", "A reason must be provided for a manual adjustment.");
}
