using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Restaurants.Commands.UpdateRestaurantLocation;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Commands;

public class UpdateRestaurantLocationTests : BaseTestFixture
{
    [Test]
    public async Task UpdateLocation_AddressOnly_Succeeds()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new UpdateRestaurantLocationCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Street: "100 Main",
            City: "Metropolis",
            State: "NY",
            ZipCode: "12345",
            Country: "US",
            Latitude: null,
            Longitude: null);

        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();

        var agg = await FindAsync<Restaurant>(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
        agg!.Location.Street.Should().Be("100 Main");
        agg.Location.City.Should().Be("Metropolis");
    }

    [Test]
    public async Task UpdateLocation_WithGeo_Succeeds()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new UpdateRestaurantLocationCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Street: "200 Center",
            City: "Gotham",
            State: "IL",
            ZipCode: "60601",
            Country: "US",
            Latitude: 40.0,
            Longitude: -73.0);

        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();

        var agg = await FindAsync<Restaurant>(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
        agg!.GeoCoordinates!.Latitude.Should().Be(40.0);
        agg.GeoCoordinates!.Longitude.Should().Be(-73.0);
    }

    [Test]
    public async Task UpdateLocation_NotFound_ReturnsError()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new UpdateRestaurantLocationCommand(
            RestaurantId: Guid.NewGuid(),
            Street: "X",
            City: "Y",
            State: "S",
            ZipCode: "Z",
            Country: "C",
            Latitude: null,
            Longitude: null);

        var result = await SendAsync(cmd);
        result.ShouldBeFailure("Restaurant.NotFound");
    }

    [Test]
    public async Task UpdateLocation_Unauthorized_ThrowsForbidden()
    {
        await RunAsDefaultUserAsync();

        var cmd = new UpdateRestaurantLocationCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Street: "X",
            City: "Y",
            State: "S",
            ZipCode: "Z",
            Country: "C",
            Latitude: null,
            Longitude: null);

        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task UpdateLocation_InvalidData_ThrowsValidation()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new UpdateRestaurantLocationCommand(
            RestaurantId: Guid.Empty,
            Street: "",
            City: "",
            State: "",
            ZipCode: "",
            Country: "",
            Latitude: 200,
            Longitude: 200);

        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ValidationException>();
    }
}

