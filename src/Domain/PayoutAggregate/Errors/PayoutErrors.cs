using YummyZoom.Domain.PayoutAggregate.Enums;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.PayoutAggregate.Errors;

public static class PayoutErrors
{
    public static readonly Error AmountMustBePositive = Error.Validation(
        "Payout.AmountMustBePositive", "Payout amount must be positive.");

    public static readonly Error IdempotencyKeyRequired = Error.Validation(
        "Payout.IdempotencyKeyRequired", "Idempotency key is required for payout creation.");

    public static readonly Error FailureReasonRequired = Error.Validation(
        "Payout.FailureReasonRequired", "Failure reason is required when marking a payout as failed.");

    public static Error InvalidStatusTransition(PayoutStatus current, PayoutStatus target) => Error.Conflict(
        "Payout.InvalidStatusTransition", $"Cannot transition payout status from '{current}' to '{target}'.");
}
