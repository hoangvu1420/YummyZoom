using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.SharedKernel;
using YummyZoom.Application.Restaurants.Commands.SetRestaurantAcceptingOrders;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

public class AcceptingOrdersContractTests
{
    [Test]
    public async Task SetAcceptingOrders_WithAuth_MapsRequest_AndReturns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<SetRestaurantAcceptingOrdersCommand>();
            var cmd = (SetRestaurantAcceptingOrdersCommand)req;
            cmd.IsAccepting.Should().BeTrue();
            return Result.Success(new SetRestaurantAcceptingOrdersResponse(true));
        });

        var restaurantId = Guid.NewGuid();
        var body = new YummyZoom.Web.Endpoints.Restaurants.SetAcceptingOrdersRequestDto(true);
        var path = $"/api/v1/restaurants/{restaurantId}/accepting-orders";
        var resp = await client.PutAsJsonAsync(path, body);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task SetAcceptingOrders_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var restaurantId = Guid.NewGuid();
        var body = new YummyZoom.Web.Endpoints.Restaurants.SetAcceptingOrdersRequestDto(true);
        var path = $"/api/v1/restaurants/{restaurantId}/accepting-orders";
        var resp = await client.PutAsJsonAsync(path, body);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

