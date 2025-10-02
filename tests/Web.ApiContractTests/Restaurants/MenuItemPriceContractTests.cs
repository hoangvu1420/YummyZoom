using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.MenuItems.Commands.UpdateMenuItemPrice;
using YummyZoom.Infrastructure.Serialization;
using YummyZoom.Infrastructure.Serialization.JsonOptions;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.Web.Endpoints;
using static YummyZoom.Web.Endpoints.Restaurants;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

public class MenuItemPriceContractTests
{
    [Test]
    public async Task UpdatePrice_WhenValid_Returns204_AndMapsCommand()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var restaurantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<UpdateMenuItemPriceCommand>();
            var cmd = (UpdateMenuItemPriceCommand)req;
            cmd.RestaurantId.Should().Be(restaurantId);
            cmd.MenuItemId.Should().Be(itemId);
            cmd.Price.Should().Be(12.34m);
            cmd.Currency.Should().Be("USD");
            return Result.Success();
        });

        var path = $"/api/v1/restaurants/{restaurantId}/menu-items/{itemId}/price";
        var body = new UpdatePriceRequestDto(12.34m, "USD");
        TestContext.WriteLine($"REQUEST PUT {path}\n{JsonSerializer.Serialize(body, DomainJson.Options)}");
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task UpdatePrice_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menu-items/{Guid.NewGuid()}/price";
        var body = new UpdatePriceRequestDto(1.0m, "USD");
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdatePrice_WhenNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.NotFound("MenuItem.MenuItemNotFound", "Missing")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menu-items/{Guid.NewGuid()}/price";
        var resp = await client.PutAsJsonAsync(path, new UpdatePriceRequestDto(1.0m, "USD"), DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdatePrice_WhenValidationFails_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.Validation("MenuItem.Invalid", "Invalid")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menu-items/{Guid.NewGuid()}/price";
        var body = new UpdatePriceRequestDto(-1.0m, "bad");
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        var raw = await resp.Content.ReadAsStringAsync();
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        raw.Should().Contain("\"status\":400").And.Contain("\"title\":\"MenuItem\"");
    }
}

