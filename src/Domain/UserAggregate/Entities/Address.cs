using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UserAggregate.Entities;

public sealed class Address : Entity<AddressId>
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string ZipCode { get; private set; }
    public string Country { get; private set; }
    public string? Label { get; private set; } // e.g., "Home", "Work"
    public string? DeliveryInstructions { get; private set; } // Optional

    private Address(
        AddressId id,
        string street,
        string city,
        string state,
        string zipCode,
        string country,
        string? label,
        string? deliveryInstructions)
        : base(id)
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
        return new Address(
            AddressId.CreateUnique(),
            street,
            city,
            state,
            zipCode,
            country,
            label,
            deliveryInstructions);
    }

    public static Address Create(
        AddressId id,
        string street,
        string city,
        string state,
        string zipCode,
        string country,
        string? label = null,
        string? deliveryInstructions = null)
    {
        return new Address(
            id,
            street,
            city,
            state,
            zipCode,
            country,
            label,
            deliveryInstructions);
    }

    public void UpdateDetails(
        string street,
        string city,
        string state,
        string zipCode,
        string country,
        string? label = null,
        string? deliveryInstructions = null)
    {
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
        Label = label;
        DeliveryInstructions = deliveryInstructions;
    }

#pragma warning disable CS8618
    // For EF Core
    private Address()
    {
    }
#pragma warning restore CS8618
}
