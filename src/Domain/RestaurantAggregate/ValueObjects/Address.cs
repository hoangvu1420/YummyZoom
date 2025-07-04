using YummyZoom.Domain.RestaurantAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAggregate.ValueObjects;

public sealed class Address : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string ZipCode { get; private set; }
    public string Country { get; private set; }

    private Address(string street, string city, string state, string zipCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }

    public static Result<Address> Create(string street, string city, string state, string zipCode, string country)
    {
        // Constants for validation
        const int maxFieldLength = 100;
        const int maxZipCodeLength = 20;

        // Validate street
        if (string.IsNullOrWhiteSpace(street))
            return Result.Failure<Address>(RestaurantErrors.AddressStreetIsRequired());
        
        if (street.Length > maxFieldLength)
            return Result.Failure<Address>(RestaurantErrors.AddressFieldTooLong("Street", maxFieldLength));

        // Validate city
        if (string.IsNullOrWhiteSpace(city))
            return Result.Failure<Address>(RestaurantErrors.AddressCityIsRequired());
        
        if (city.Length > maxFieldLength)
            return Result.Failure<Address>(RestaurantErrors.AddressFieldTooLong("City", maxFieldLength));

        // Validate state
        if (string.IsNullOrWhiteSpace(state))
            return Result.Failure<Address>(RestaurantErrors.AddressStateIsRequired());
        
        if (state.Length > maxFieldLength)
            return Result.Failure<Address>(RestaurantErrors.AddressFieldTooLong("State", maxFieldLength));

        // Validate zipCode
        if (string.IsNullOrWhiteSpace(zipCode))
            return Result.Failure<Address>(RestaurantErrors.AddressZipCodeIsRequired());
        
        if (zipCode.Length > maxZipCodeLength)
            return Result.Failure<Address>(RestaurantErrors.AddressFieldTooLong("ZipCode", maxZipCodeLength));

        // Validate country
        if (string.IsNullOrWhiteSpace(country))
            return Result.Failure<Address>(RestaurantErrors.AddressCountryIsRequired());
        
        if (country.Length > maxFieldLength)
            return Result.Failure<Address>(RestaurantErrors.AddressFieldTooLong("Country", maxFieldLength));

        return Result.Success(new Address(street.Trim(), city.Trim(), state.Trim(), zipCode.Trim(), country.Trim()));
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
    private Address() { }
#pragma warning restore CS8618
}
