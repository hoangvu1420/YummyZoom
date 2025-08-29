using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.Web.Endpoints;
using YummyZoom.SharedKernel;
using YummyZoom.Infrastructure.Serialization;
using YummyZoom.Application.MenuItems.Commands.CreateMenuItem;
using YummyZoom.Application.MenuItems.Commands.ChangeMenuItemAvailability;
using YummyZoom.Application.MenuItems.Commands.UpdateMenuItemDetails;
using YummyZoom.Application.MenuItems.Commands.AssignMenuItemToCategory;
using YummyZoom.Application.MenuItems.Commands.UpdateMenuItemDietaryTags;
using YummyZoom.Application.MenuItems.Commands.DeleteMenuItem;
using static YummyZoom.Web.Endpoints.Restaurants;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

public class MenuItemsManagementContractTests
{
    #region POST menu-items
    [Test]
    public async Task CreateMenuItem_WhenValid_Returns201_AndMapsCommand()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var restaurantId = Guid.NewGuid();
        var expectedItemId = Guid.NewGuid();

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<CreateMenuItemCommand>();
            var cmd = (CreateMenuItemCommand)req;
            cmd.RestaurantId.Should().Be(restaurantId);
            cmd.Name.Should().Be("Burger");
            return Result.Success(new CreateMenuItemResponse(expectedItemId));
        });

        var body = new CreateMenuItemRequestDto(
            MenuCategoryId: Guid.NewGuid(),
            Name: "Burger",
            Description: "Juicy",
            Price: 9.99m,
            Currency: "USD",
            ImageUrl: null,
            IsAvailable: true,
            DietaryTagIds: new List<Guid> { Guid.NewGuid() }
        );

        var path = $"/api/v1/restaurants/{restaurantId}/menu-items";
        TestContext.WriteLine($"REQUEST POST {path}\n{JsonSerializer.Serialize(body, DomainJson.Options)}");
        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        resp.Headers.Location!.ToString().Should().Contain($"/api/v1/restaurants/{restaurantId}/menu-items/");
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        var dto = JsonSerializer.Deserialize<CreateMenuItemResponse>(raw, DomainJson.Options);
        dto!.MenuItemId.Should().Be(expectedItemId);
    }

    [Test]
    public async Task CreateMenuItem_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var restaurantId = Guid.NewGuid();
        var body = new CreateMenuItemRequestDto(Guid.NewGuid(), "Name", "D", 1m, "USD", null, true, null);
        var path = $"/api/v1/restaurants/{restaurantId}/menu-items";
        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateMenuItem_WhenValidationFails_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure<CreateMenuItemResponse>(Error.Validation("MenuItem.Invalid", "Invalid")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menu-items";
        var body = new CreateMenuItemRequestDto(Guid.NewGuid(), "", "", -1m, "", null, true, null);
        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        raw.Should().Contain("\"status\":400").And.Contain("\"title\":\"MenuItem\"");
    }

    [Test]
    public async Task CreateMenuItem_WhenCategoryNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure<CreateMenuItemResponse>(Error.NotFound("MenuItem.CategoryNotFound", "Missing")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menu-items";
        var body = new CreateMenuItemRequestDto(Guid.NewGuid(), "Name", "D", 1m, "USD", null, true, null);
        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PUT availability
    [Test]
    public async Task ChangeAvailability_WhenValid_Returns204_AndMapsCommand()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var restaurantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<ChangeMenuItemAvailabilityCommand>();
            var cmd = (ChangeMenuItemAvailabilityCommand)req;
            cmd.RestaurantId.Should().Be(restaurantId);
            cmd.MenuItemId.Should().Be(itemId);
            cmd.IsAvailable.Should().BeFalse();
            return Result.Success();
        });
        var path = $"/api/v1/restaurants/{restaurantId}/menu-items/{itemId}/availability";
        var body = new UpdateAvailabilityRequestDto(false);
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task ChangeAvailability_WhenNotFound_Returns404()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.NotFound("MenuItem.MenuItemNotFound", "Missing")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menu-items/{Guid.NewGuid()}/availability";
        var resp = await client.PutAsJsonAsync(path, new UpdateAvailabilityRequestDto(true), DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PUT details
    [Test]
    public async Task UpdateDetails_WhenValid_Returns204_AndMapsCommand()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var restaurantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<UpdateMenuItemDetailsCommand>();
            var cmd = (UpdateMenuItemDetailsCommand)req;
            cmd.RestaurantId.Should().Be(restaurantId);
            cmd.MenuItemId.Should().Be(itemId);
            cmd.Price.Should().Be(12.34m);
            cmd.Currency.Should().Be("USD");
            return Result.Success();
        });
        var body = new UpdateMenuItemRequestDto("New", "Desc", 12.34m, "USD", null);
        var path = $"/api/v1/restaurants/{restaurantId}/menu-items/{itemId}";
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task UpdateDetails_WhenValidationFails_Returns400()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.Validation("MenuItem.Invalid", "Invalid")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menu-items/{Guid.NewGuid()}";
        var body = new UpdateMenuItemRequestDto("", "", -1m, "", null);
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT assign category
    [Test]
    public async Task AssignCategory_WhenValid_Returns204_AndMapsCommand()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var restaurantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<AssignMenuItemToCategoryCommand>();
            var cmd = (AssignMenuItemToCategoryCommand)req;
            cmd.RestaurantId.Should().Be(restaurantId);
            cmd.MenuItemId.Should().Be(itemId);
            cmd.NewCategoryId.Should().Be(newCategoryId);
            return Result.Success();
        });
        var path = $"/api/v1/restaurants/{restaurantId}/menu-items/{itemId}/category";
        var body = new AssignMenuItemToCategoryRequestDto(newCategoryId);
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task AssignCategory_WhenConflict_Returns409Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.Conflict("MenuItem.CategoryNotBelongsToRestaurant", "Wrong scope")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menu-items/{Guid.NewGuid()}/category";
        var resp = await client.PutAsJsonAsync(path, new AssignMenuItemToCategoryRequestDto(Guid.NewGuid()), DomainJson.Options);
        var raw = await resp.Content.ReadAsStringAsync();
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        raw.Should().Contain("\"status\":409").And.Contain("\"title\":\"MenuItem\"");
    }

    #endregion

    #region PUT dietary tags
    [Test]
    public async Task UpdateDietaryTags_WhenValid_Returns204_AndMapsCommand()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var restaurantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var tags = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<UpdateMenuItemDietaryTagsCommand>();
            var cmd = (UpdateMenuItemDietaryTagsCommand)req;
            cmd.RestaurantId.Should().Be(restaurantId);
            cmd.MenuItemId.Should().Be(itemId);
            cmd.DietaryTagIds.Should().BeEquivalentTo(tags);
            return Result.Success();
        });
        var path = $"/api/v1/restaurants/{restaurantId}/menu-items/{itemId}/dietary-tags";
        var body = new UpdateDietaryTagsRequestDto(tags);
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task UpdateDietaryTags_WhenValidationFails_Returns400()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.Validation("MenuItem.InvalidTags", "Invalid")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menu-items/{Guid.NewGuid()}/dietary-tags";
        var resp = await client.PutAsJsonAsync(path, new UpdateDietaryTagsRequestDto(new List<Guid> { Guid.Empty }), DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region DELETE menu item
    [Test]
    public async Task DeleteMenuItem_WhenValid_Returns204_AndMapsCommand()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var restaurantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<DeleteMenuItemCommand>();
            var cmd = (DeleteMenuItemCommand)req;
            cmd.RestaurantId.Should().Be(restaurantId);
            cmd.MenuItemId.Should().Be(itemId);
            return Result.Success();
        });
        var path = $"/api/v1/restaurants/{restaurantId}/menu-items/{itemId}";
        var resp = await client.DeleteAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteMenuItem_WhenNotFound_Returns404()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.NotFound("MenuItem.MenuItemNotFound", "Missing")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menu-items/{Guid.NewGuid()}";
        var resp = await client.DeleteAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    #endregion
}

