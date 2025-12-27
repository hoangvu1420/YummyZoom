using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.Restaurants.Queries.Management.GetRestaurantProfile;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public class GetRestaurantProfileQueryTests : BaseTestFixture
{
    [Test]
    public async Task Profile_ShouldReturnRestaurantDetails()
    {
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        await RunAsRestaurantStaffAsync("profile@restaurant.com", restaurantId);

        var result = await SendAsync(new GetRestaurantProfileQuery(restaurantId));

        result.ShouldBeSuccessful();
        result.Value.RestaurantId.Should().Be(restaurantId);
        result.Value.Name.Should().Be(DefaultTestData.Restaurant.Name);
        result.Value.Description.Should().Be(DefaultTestData.Restaurant.Description);
        result.Value.LogoUrl.Should().Be(DefaultTestData.Restaurant.LogoUrl);
        result.Value.Phone.Should().Be(DefaultTestData.Restaurant.Contact.Phone);
        result.Value.Email.Should().Be(DefaultTestData.Restaurant.Contact.Email);
        result.Value.BusinessHours.Should().Be(DefaultTestData.Restaurant.Hours.BusinessHours);
        result.Value.Address.Street.Should().Be(DefaultTestData.Restaurant.Address.Street);
        result.Value.Address.City.Should().Be(DefaultTestData.Restaurant.Address.City);
        result.Value.Address.State.Should().Be(DefaultTestData.Restaurant.Address.State);
        result.Value.Address.ZipCode.Should().Be(DefaultTestData.Restaurant.Address.ZipCode);
        result.Value.Address.Country.Should().Be(DefaultTestData.Restaurant.Address.Country);
        result.Value.Latitude.Should().BeApproximately(DefaultTestData.Restaurant.GeoCoordinates.Latitude, 0.0001);
        result.Value.Longitude.Should().BeApproximately(DefaultTestData.Restaurant.GeoCoordinates.Longitude, 0.0001);
        result.Value.IsAcceptingOrders.Should().BeTrue();
        result.Value.IsVerified.Should().BeTrue();
    }

    [Test]
    public async Task Profile_ShouldReturnNotFoundForMissingRestaurant()
    {
        var restaurantId = await TestDataFactory.CreateInactiveRestaurantAsync();
        await RunAsRestaurantStaffAsync("profile-missing@restaurant.com", restaurantId);

        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"Restaurants\" SET \"IsDeleted\" = TRUE WHERE \"Id\" = {restaurantId};");
        });

        var result = await SendAsync(new GetRestaurantProfileQuery(restaurantId));

        result.Error.Should().Be(GetRestaurantProfileErrors.NotFound(restaurantId));
    }
}
