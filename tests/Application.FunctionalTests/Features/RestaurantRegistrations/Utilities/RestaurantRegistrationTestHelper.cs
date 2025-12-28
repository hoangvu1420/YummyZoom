using YummyZoom.Application.RestaurantRegistrations.Commands.SubmitRestaurantRegistration;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.Features.RestaurantRegistrations.Utilities;

internal static class RestaurantRegistrationTestHelper
{
    public static SubmitRestaurantRegistrationCommand BuildValidSubmitCommand(UserId userId,
        string name = "Test Resto",
        string description = "Cozy place",
        string cuisine = "Italian",
        string city = "Seattle")
    {
        return new SubmitRestaurantRegistrationCommand(
            Name: name,
            Description: description,
            CuisineType: cuisine,
            Street: "1 Main St",
            City: city,
            State: "WA",
            ZipCode: "98101",
            Country: "US",
            PhoneNumber: "+12065550123",
            Email: "owner@example.com",
            BusinessHours: "09:00-17:00",
            LogoUrl: "https://example.com/logo.png",
            Latitude: 47.61,
            Longitude: -122.33)
        { UserId = userId };
    }
}
