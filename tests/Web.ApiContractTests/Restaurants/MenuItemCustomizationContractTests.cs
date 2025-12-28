using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.MenuItems.Commands.AssignCustomizationGroupToMenuItem;
using YummyZoom.Application.MenuItems.Commands.RemoveCustomizationGroupFromMenuItem;
using YummyZoom.Infrastructure.Serialization;
using YummyZoom.Infrastructure.Serialization.JsonOptions;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.Web.Endpoints;
using static YummyZoom.Web.Endpoints.Restaurants;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

public class MenuItemCustomizationContractTests
{
    #region POST customizations
    [Test]
    public async Task AssignCustomization_WhenValid_Returns204_AndMapsCommand()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var restaurantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<AssignCustomizationGroupToMenuItemCommand>();
            var cmd = (AssignCustomizationGroupToMenuItemCommand)req;
            cmd.RestaurantId.Should().Be(restaurantId);
            cmd.MenuItemId.Should().Be(itemId);
            cmd.CustomizationGroupId.Should().Be(groupId);
            cmd.DisplayTitle.Should().Be("Add-ons");
            cmd.DisplayOrder.Should().Be(2);
            return Result.Success();
        });

        var body = new AssignCustomizationRequestDto(GroupId: groupId, DisplayTitle: "Add-ons", DisplayOrder: 2);
        var path = $"/api/v1/restaurants/{restaurantId}/menu-items/{itemId}/customizations";
        TestContext.WriteLine($"REQUEST POST {path}\n{JsonSerializer.Serialize(body, DomainJson.Options)}");
        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task AssignCustomization_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menu-items/{Guid.NewGuid()}/customizations";
        var body = new AssignCustomizationRequestDto(Guid.NewGuid(), "Title", 1);
        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AssignCustomization_WhenGroupNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.NotFound("CustomizationGroup.NotFound", "Missing")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menu-items/{Guid.NewGuid()}/customizations";
        var body = new AssignCustomizationRequestDto(Guid.NewGuid(), "Title", 1);
        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AssignCustomization_WhenValidationFails_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.Validation("MenuItem.InvalidCustomization", "Invalid")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menu-items/{Guid.NewGuid()}/customizations";
        var body = new AssignCustomizationRequestDto(Guid.Empty, "", -1);
        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);
        var raw = await resp.Content.ReadAsStringAsync();
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = JsonSerializer.Deserialize<Microsoft.AspNetCore.Mvc.ProblemDetails>(raw);
        problem!.Status.Should().Be(400);
        problem.Title.Should().Be("MenuItem.InvalidCustomization");
    }
    #endregion

    #region DELETE customizations
    [Test]
    public async Task RemoveCustomization_WhenValid_Returns204_AndMapsCommand()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var restaurantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<RemoveCustomizationGroupFromMenuItemCommand>();
            var cmd = (RemoveCustomizationGroupFromMenuItemCommand)req;
            cmd.RestaurantId.Should().Be(restaurantId);
            cmd.MenuItemId.Should().Be(itemId);
            cmd.CustomizationGroupId.Should().Be(groupId);
            return Result.Success();
        });

        var path = $"/api/v1/restaurants/{restaurantId}/menu-items/{itemId}/customizations/{groupId}";
        TestContext.WriteLine($"REQUEST DELETE {path}");
        var resp = await client.DeleteAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task RemoveCustomization_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menu-items/{Guid.NewGuid()}/customizations/{Guid.NewGuid()}";
        var resp = await client.DeleteAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task RemoveCustomization_WhenNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.NotFound("MenuItem.CustomizationNotFound", "Not assigned")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menu-items/{Guid.NewGuid()}/customizations/{Guid.NewGuid()}";
        var resp = await client.DeleteAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    #endregion
}
