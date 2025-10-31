using System.Net;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.Restaurants.Queries.Public.GetMenuItemAvailability;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

[TestFixture]
public class MenuItemAvailabilityContractTests
{
    [Test]
    public async Task GetAvailability_WhenFound_ReturnsPayload_WithShortCache()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var restaurantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var dto = new MenuItemAvailabilityDto(restaurantId, itemId, true, null, DateTimeOffset.UtcNow, 15);

        factory.Sender.RespondWith(req => req switch
        {
            GetMenuItemAvailabilityQuery q when q.RestaurantId == restaurantId && q.ItemId == itemId => Result.Success(dto),
            _ => throw new AssertionException("Unexpected request")
        });

        var resp = await client.GetAsync($"/api/v1/restaurants/{restaurantId}/menu-items/{itemId}/availability");
        var raw = await resp.Content.ReadAsStringAsync();

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Headers.CacheControl?.Public.Should().BeTrue();
        resp.Headers.CacheControl?.MaxAge.Should().Be(TimeSpan.FromSeconds(15));
        raw.Should().Contain("\"isAvailable\":true");
    }

    [Test]
    public async Task GetAvailability_WhenNotFound_Returns404()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var restaurantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        factory.Sender.RespondWith(req => req switch
        {
            GetMenuItemAvailabilityQuery q when q.RestaurantId == restaurantId && q.ItemId == itemId =>
                Result.Failure<MenuItemAvailabilityDto>(Error.NotFound("Public.MenuItemAvailability.NotFound", "Missing")),
            _ => throw new AssertionException("Unexpected request")
        });

        var resp = await client.GetAsync($"/api/v1/restaurants/{restaurantId}/menu-items/{itemId}/availability");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

