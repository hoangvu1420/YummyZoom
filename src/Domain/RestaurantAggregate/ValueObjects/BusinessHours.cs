
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAggregate.ValueObjects;

public sealed class BusinessHours : ValueObject
{
    // For simplicity, we'll use a string to represent business hours.
    // This could be expanded to a more complex type if needed.
    public string Hours { get; private set; }

    private BusinessHours(string hours)
    {
        Hours = hours;
    }

    public static BusinessHours Create(string hours)
    {
        return new BusinessHours(hours);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Hours;
    }

#pragma warning disable CS8618
    private BusinessHours() { }
#pragma warning restore CS8618
}
