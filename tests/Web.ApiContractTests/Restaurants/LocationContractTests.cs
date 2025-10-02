using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.Restaurants.Commands.UpdateRestaurantLocation;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

public class LocationContractTests
{
    [Test]
    public async Task UpdateLocation_WithAuth_MapsRequest_AndReturns204()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<UpdateRestaurantLocationCommand>();
            var cmd = (UpdateRestaurantLocationCommand)req;
            cmd.Street.Should().Be("100 Main");
            cmd.City.Should().Be("Metro");
            return Result.Success();
        });

        var restaurantId = Guid.NewGuid();
        var body = new YummyZoom.Web.Endpoints.Restaurants.UpdateLocationRequestDto("100 Main", "Metro", "NY", "12345", "US", null, null);
        var path = $"/api/v1/restaurants/{restaurantId}/location";
        var resp = await client.PutAsJsonAsync(path, body);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task UpdateLocation_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var restaurantId = Guid.NewGuid();
        var body = new YummyZoom.Web.Endpoints.Restaurants.UpdateLocationRequestDto("100 Main", "Metro", "NY", "12345", "US", null, null);
        var path = $"/api/v1/restaurants/{restaurantId}/location";
        var resp = await client.PutAsJsonAsync(path, body);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

