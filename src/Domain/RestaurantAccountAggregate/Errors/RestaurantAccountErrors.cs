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

    public static Error PayoutHoldMustBePositive(Money amount) => Error.Validation(
        "RestaurantAccount.PayoutHoldMustBePositive", $"Payout hold amount '{amount}' must be positive.");

    public static Error InsufficientAvailableBalance(Money availableBalance, Money holdAmount) => Error.Validation(
        "RestaurantAccount.InsufficientAvailableBalance", $"Available balance '{availableBalance}' is insufficient for payout hold of '{holdAmount}'.");

    public static Error InsufficientPayoutHold(Money pendingHold, Money releaseAmount) => Error.Validation(
        "RestaurantAccount.InsufficientPayoutHold", $"Pending payout hold '{pendingHold}' is insufficient to release '{releaseAmount}'.");

    public static Error PayoutCurrencyMismatch(string accountCurrency, string payoutCurrency) => Error.Validation(
        "RestaurantAccount.PayoutCurrencyMismatch", $"Payout currency '{payoutCurrency}' does not match account currency '{accountCurrency}'.");

    public static Error InsufficientBalance(Money currentBalance, Money payoutAmount) => Error.Validation(
        "RestaurantAccount.InsufficientBalance", $"Insufficient balance '{currentBalance}' for payout settlement of '{payoutAmount}'.");

    public static readonly Error ManualAdjustmentReasonRequired = Error.Validation(
        "RestaurantAccount.ManualAdjustmentReasonRequired", "A reason must be provided for a manual adjustment.");
}
