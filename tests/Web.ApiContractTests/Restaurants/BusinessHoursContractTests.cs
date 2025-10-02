using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.SharedKernel;
using YummyZoom.Application.Restaurants.Commands.UpdateRestaurantBusinessHours;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

public class BusinessHoursContractTests
{
    [Test]
    public async Task UpdateBusinessHours_WithAuth_MapsRequest_AndReturns204()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<UpdateRestaurantBusinessHoursCommand>();
            var cmd = (UpdateRestaurantBusinessHoursCommand)req;
            cmd.BusinessHours.Should().Be("09:00-17:00");
            return Result.Success();
        });

        var restaurantId = Guid.NewGuid();
        var body = new YummyZoom.Web.Endpoints.Restaurants.UpdateBusinessHoursRequestDto("09:00-17:00");
        var path = $"/api/v1/restaurants/{restaurantId}/business-hours";
        var resp = await client.PutAsJsonAsync(path, body);

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task UpdateBusinessHours_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var restaurantId = Guid.NewGuid();
        var body = new YummyZoom.Web.Endpoints.Restaurants.UpdateBusinessHoursRequestDto("09:00-17:00");
        var path = $"/api/v1/restaurants/{restaurantId}/business-hours";
        var resp = await client.PutAsJsonAsync(path, body);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
