
namespace YummyZoom.Domain.Common.ValueObjects;

public sealed class Address : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string ZipCode { get; private set; }
    public string Country { get; private set; }
    public string? Label { get; private set; } // Optional
    public string? DeliveryInstructions { get; private set; } // Optional

    private Address(
        string street,
        string city,
        string state,
        string zipCode,
        string country,
        string? label,
        string? deliveryInstructions)
    {
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
        Label = label;
        DeliveryInstructions = deliveryInstructions;
    }

    public static Address Create(
        string street,
        string city,
        string state,
        string zipCode,
        string country,
        string? label = null,
        string? deliveryInstructions = null)
    {
        // Structural/Format validation is handled in higher layers (e.g., Application)
        // Domain layer assumes valid input for creating the Value Object.
        return new Address(
            street,
            city,
            state,
            zipCode,
            country,
            label,
            deliveryInstructions);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return ZipCode;
        yield return Country;
        yield return Label ?? NullPlaceholder; 
        yield return DeliveryInstructions ?? NullPlaceholder; 
    }

    private static readonly object NullPlaceholder = new object();

#pragma warning disable CS8618
    // For EF Core
    private Address()
    {
    }
#pragma warning restore CS8618
}
