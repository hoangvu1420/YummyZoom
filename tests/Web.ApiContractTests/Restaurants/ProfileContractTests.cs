using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.SharedKernel;
using YummyZoom.Application.Restaurants.Commands.UpdateRestaurantProfile;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

public class ProfileContractTests
{
    [Test]
    public async Task UpdateProfile_WithAuth_MapsRequest_AndReturns204()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<UpdateRestaurantProfileCommand>();
            var cmd = (UpdateRestaurantProfileCommand)req;
            cmd.Name.Should().Be("N");
            cmd.Description.Should().Be("D");
            return Result.Success();
        });

        var restaurantId = Guid.NewGuid();
        var body = new YummyZoom.Web.Endpoints.Restaurants.UpdateProfileRequestDto("N", "D", null, null, null);
        var path = $"/api/v1/restaurants/{restaurantId}/profile";
        var resp = await client.PutAsJsonAsync(path, body);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task UpdateProfile_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var restaurantId = Guid.NewGuid();
        var body = new YummyZoom.Web.Endpoints.Restaurants.UpdateProfileRequestDto("N", null, null, null, null);
        var path = $"/api/v1/restaurants/{restaurantId}/profile";
        var resp = await client.PutAsJsonAsync(path, body);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

