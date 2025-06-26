
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.Common.Errors;

public static class Money
{
    public static readonly Error NegativeAmount = Error.Validation(
        "Money.NegativeAmount", "Money amount cannot be negative.");

    public static readonly Error InvalidCurrency = Error.Validation(
        "Money.InvalidCurrency", "A currency must be provided.");
}
