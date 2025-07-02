
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.Common.Errors;

public static class MoneyErrors
{
    public static readonly Error InvalidCurrency = Error.Validation(
        "Money.InvalidCurrency", "A currency must be provided.");
}
