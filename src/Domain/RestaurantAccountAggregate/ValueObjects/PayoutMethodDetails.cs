using YummyZoom.Domain.RestaurantAccountAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAccountAggregate.ValueObjects;

public sealed class PayoutMethodDetails : ValueObject
{
    public string Details { get; private set; }

    private PayoutMethodDetails(string details)
    {
        Details = details;
    }

    public static Result<PayoutMethodDetails> Create(string details)
    {
        if (string.IsNullOrWhiteSpace(details))
        {
            return Result.Failure<PayoutMethodDetails>(RestaurantAccountErrors.InvalidPayoutMethod);
        }

        return Result.Success(new PayoutMethodDetails(details.Trim()));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Details;
    }

#pragma warning disable CS8618
    private PayoutMethodDetails() { }
#pragma warning restore CS8618
}
