
using YummyZoom.Domain.RestaurantAggregate.Events;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.RestaurantAggregate;

public sealed class Restaurant : AggregateRoot<RestaurantId, Guid>
{
    public string Name { get; private set; }
    public string LogoUrl { get; private set; }
    public string Description { get; private set; }
    public string CuisineType { get; private set; }
    public Address Location { get; private set; }
    public ContactInfo ContactInfo { get; private set; }
    public BusinessHours BusinessHours { get; private set; }
    public bool IsVerified { get; private set; }
    public bool IsAcceptingOrders { get; private set; }

    private Restaurant(
        RestaurantId id,
        string name,
        string logoUrl,
        string description,
        string cuisineType,
        Address location,
        ContactInfo contactInfo,
        BusinessHours businessHours,
        bool isVerified,
        bool isAcceptingOrders)
        : base(id)
    {
        Name = name;
        LogoUrl = logoUrl;
        Description = description;
        CuisineType = cuisineType;
        Location = location;
        ContactInfo = contactInfo;
        BusinessHours = businessHours;
        IsVerified = isVerified;
        IsAcceptingOrders = isAcceptingOrders;
    }

    public static Restaurant Create(
        string name,
        string logoUrl,
        string description,
        string cuisineType,
        Address location,
        ContactInfo contactInfo,
        BusinessHours businessHours)
    {
        var restaurant = new Restaurant(
            RestaurantId.CreateUnique(),
            name,
            logoUrl,
            description,
            cuisineType,
            location,
            contactInfo,
            businessHours,
            isVerified: false,
            isAcceptingOrders: false);

        restaurant.AddDomainEvent(new RestaurantCreated((RestaurantId)restaurant.Id));

        return restaurant;
    }

    public void UpdateDetails(string name, string description, string cuisineType, string logoUrl, Address location, ContactInfo contactInfo, BusinessHours businessHours)
    {
        Name = name;
        Description = description;
        CuisineType = cuisineType;
        LogoUrl = logoUrl;
        Location = location;
        ContactInfo = contactInfo;
        BusinessHours = businessHours;

        AddDomainEvent(new RestaurantUpdated((RestaurantId)Id));
    }

    public void Verify()
    {
        if (IsVerified) return;
        IsVerified = true;
        AddDomainEvent(new RestaurantVerified((RestaurantId)Id));
    }

    public void AcceptOrders()
    {
        if (IsAcceptingOrders) return;
        IsAcceptingOrders = true;
        AddDomainEvent(new RestaurantAcceptingOrders((RestaurantId)Id));
    }

    public void DeclineOrders()
    {
        if (!IsAcceptingOrders) return;
        IsAcceptingOrders = false;
        AddDomainEvent(new RestaurantNotAcceptingOrders((RestaurantId)Id));
    }

#pragma warning disable CS8618
    private Restaurant() { }
#pragma warning restore CS8618
}
