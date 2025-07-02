using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.OrderAggregate.ValueObjects;

public sealed class DeliveryAddress : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string ZipCode { get; private set; }
    public string Country { get; private set; }

    private DeliveryAddress(string street, string city, string state, string zipCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }

    public static Result<DeliveryAddress> Create(string street, string city, string state, string zipCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street) ||
            string.IsNullOrWhiteSpace(city) ||
            string.IsNullOrWhiteSpace(state) ||
            string.IsNullOrWhiteSpace(zipCode) ||
            string.IsNullOrWhiteSpace(country))
        {
            // In a real application, you would have more specific errors.
            return Result.Failure<DeliveryAddress>(OrderErrors.AddressInvalid);
        }

        return new DeliveryAddress(street, city, state, zipCode, country);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return ZipCode;
        yield return Country;
    }

#pragma warning disable CS8618
    private DeliveryAddress() { }
#pragma warning restore CS8618
}
