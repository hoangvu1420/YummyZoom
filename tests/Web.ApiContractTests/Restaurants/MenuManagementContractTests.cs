using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.Menus.Commands.ChangeMenuAvailability;
using YummyZoom.Application.Menus.Commands.CreateMenu;
using YummyZoom.Application.Menus.Commands.UpdateMenuDetails;
using YummyZoom.Infrastructure.Serialization;
using YummyZoom.Infrastructure.Serialization.JsonOptions;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.Web.Endpoints;
using static YummyZoom.Web.Endpoints.Restaurants;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

public class MenuManagementContractTests
{
    #region POST menus
    [Test]
    public async Task CreateMenu_WhenValid_Returns201_AndMapsCommand()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var restaurantId = Guid.NewGuid();
        var expectedMenuId = Guid.NewGuid();

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<CreateMenuCommand>();
            var cmd = (CreateMenuCommand)req;
            cmd.RestaurantId.Should().Be(restaurantId);
            cmd.Name.Should().Be("Dinner Menu");
            cmd.Description.Should().Be("Our evening dinner selection");
            cmd.IsEnabled.Should().BeTrue();
            return Result.Success(new CreateMenuResponse(expectedMenuId));
        });

        var body = new CreateMenuRequestDto(
            Name: "Dinner Menu",
            Description: "Our evening dinner selection",
            IsEnabled: true
        );

        var path = $"/api/v1/restaurants/{restaurantId}/menus";
        TestContext.WriteLine($"REQUEST POST {path}\n{JsonSerializer.Serialize(body, DomainJson.Options)}");
        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        resp.Headers.Location!.ToString().Should().Contain($"/api/v1/restaurants/{restaurantId}/menus/");
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        var dto = JsonSerializer.Deserialize<CreateMenuResponse>(raw, DomainJson.Options);
        dto!.MenuId.Should().Be(expectedMenuId);
    }

    [Test]
    public async Task CreateMenu_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var restaurantId = Guid.NewGuid();
        var body = new CreateMenuRequestDto("Menu", "Description", true);
        var path = $"/api/v1/restaurants/{restaurantId}/menus";
        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateMenu_WhenValidationFails_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure<CreateMenuResponse>(Error.Validation("Menu.InvalidName", "Name is required")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menus";
        var body = new CreateMenuRequestDto("", "", true);
        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        raw.Should().Contain("\"status\":400").And.Contain("\"title\":\"Menu\"");
    }

    [Test]
    public async Task CreateMenu_WhenRestaurantNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure<CreateMenuResponse>(Error.NotFound("Restaurant.NotFound", "Restaurant not found")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menus";
        var body = new CreateMenuRequestDto("Valid Menu", "Valid Description", true);
        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PUT menu details
    [Test]
    public async Task UpdateMenuDetails_WhenValid_Returns204_AndMapsCommand()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var restaurantId = Guid.NewGuid();
        var menuId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<UpdateMenuDetailsCommand>();
            var cmd = (UpdateMenuDetailsCommand)req;
            cmd.RestaurantId.Should().Be(restaurantId);
            cmd.MenuId.Should().Be(menuId);
            cmd.Name.Should().Be("Updated Menu");
            cmd.Description.Should().Be("Updated description");
            return Result.Success();
        });
        var body = new UpdateMenuDetailsRequestDto("Updated Menu", "Updated description");
        var path = $"/api/v1/restaurants/{restaurantId}/menus/{menuId}";
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task UpdateMenuDetails_WhenNotFound_Returns404()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.NotFound("Menu.InvalidMenuId", "Menu not found")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menus/{Guid.NewGuid()}";
        var body = new UpdateMenuDetailsRequestDto("Name", "Description");
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateMenuDetails_WhenValidationFails_Returns400()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.Validation("Menu.InvalidName", "Name cannot be empty")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menus/{Guid.NewGuid()}";
        var body = new UpdateMenuDetailsRequestDto("", "");
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT menu availability
    [Test]
    public async Task ChangeMenuAvailability_WhenValid_Returns204_AndMapsCommand()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var restaurantId = Guid.NewGuid();
        var menuId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<ChangeMenuAvailabilityCommand>();
            var cmd = (ChangeMenuAvailabilityCommand)req;
            cmd.RestaurantId.Should().Be(restaurantId);
            cmd.MenuId.Should().Be(menuId);
            cmd.IsEnabled.Should().BeFalse();
            return Result.Success();
        });
        var path = $"/api/v1/restaurants/{restaurantId}/menus/{menuId}/availability";
        var body = new UpdateMenuAvailabilityRequestDto(false);
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task ChangeMenuAvailability_WhenNotFound_Returns404()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.NotFound("Menu.InvalidMenuId", "Menu not found")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menus/{Guid.NewGuid()}/availability";
        var resp = await client.PutAsJsonAsync(path, new UpdateMenuAvailabilityRequestDto(true), DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ChangeMenuAvailability_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menus/{Guid.NewGuid()}/availability";
        var body = new UpdateMenuAvailabilityRequestDto(true);
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
