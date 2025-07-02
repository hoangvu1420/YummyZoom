using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAccountAggregate.Errors;

public static class RestaurantAccountErrors
{
    public static readonly Error InvalidId = Error.Validation(
        "RestaurantAccount.InvalidId", "Restaurant account ID is invalid.");

    public static readonly Error InvalidPayoutMethod = Error.Validation(
        "RestaurantAccount.InvalidPayoutMethod", "Payout method details cannot be empty.");

    public static readonly Error OrderRevenueMustBePositive = Error.Validation(
        "RestaurantAccount.OrderRevenueMustBePositive", "Order revenue must be a positive amount.");

    public static readonly Error PlatformFeeMustBeNegative = Error.Validation(
        "RestaurantAccount.PlatformFeeMustBeNegative", "Platform fee must be a negative amount.");

    public static readonly Error RefundDeductionMustBeNegative = Error.Validation(
        "RestaurantAccount.RefundDeductionMustBeNegative", "Refund deduction must be a negative amount.");

    public static readonly Error PayoutSettlementMustBeNegative = Error.Validation(
        "RestaurantAccount.PayoutSettlementMustBeNegative", "Payout settlement must be a negative amount.");

    public static readonly Error InsufficientBalance = Error.Validation(
        "RestaurantAccount.InsufficientBalance", "Insufficient balance for payout settlement.");

    public static readonly Error BalanceInconsistency = Error.Validation(
        "RestaurantAccount.BalanceInconsistency", "Current balance does not match the sum of all transactions.");
}
