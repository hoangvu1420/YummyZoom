using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.Web.Endpoints;
using YummyZoom.SharedKernel;
using YummyZoom.Infrastructure.Serialization;
using YummyZoom.Application.MenuCategories.Commands.AddMenuCategory;
using YummyZoom.Application.MenuCategories.Commands.UpdateMenuCategoryDetails;
using YummyZoom.Application.MenuCategories.Commands.RemoveMenuCategory;
using YummyZoom.Infrastructure.Serialization.JsonOptions;
using static YummyZoom.Web.Endpoints.Restaurants;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

public class MenuCategoryManagementContractTests
{
    #region POST menu categories
    
    [Test]
    public async Task AddMenuCategory_WhenValid_Returns201_AndMapsCommand()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var restaurantId = Guid.NewGuid();
        var menuId = Guid.NewGuid();
        var expectedCategoryId = Guid.NewGuid();

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<AddMenuCategoryCommand>();
            var cmd = (AddMenuCategoryCommand)req;
            cmd.RestaurantId.Should().Be(restaurantId);
            cmd.MenuId.Should().Be(menuId);
            cmd.Name.Should().Be("Appetizers");
            return Result.Success(new AddMenuCategoryResponse(expectedCategoryId));
        });

        var body = new AddMenuCategoryRequestDto(Name: "Appetizers");

        var path = $"/api/v1/restaurants/{restaurantId}/menus/{menuId}/categories";
        TestContext.WriteLine($"REQUEST POST {path}\n{JsonSerializer.Serialize(body, DomainJson.Options)}");
        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        resp.Headers.Location!.ToString().Should().Contain($"/api/v1/restaurants/{restaurantId}/categories/");
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        var dto = JsonSerializer.Deserialize<AddMenuCategoryResponse>(raw, DomainJson.Options);
        dto!.MenuCategoryId.Should().Be(expectedCategoryId);
    }

    [Test]
    public async Task AddMenuCategory_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var restaurantId = Guid.NewGuid();
        var menuId = Guid.NewGuid();
        var body = new AddMenuCategoryRequestDto("Category");
        var path = $"/api/v1/restaurants/{restaurantId}/menus/{menuId}/categories";
        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AddMenuCategory_WhenValidationFails_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure<AddMenuCategoryResponse>(Error.Validation("MenuCategory.InvalidName", "Name is required")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menus/{Guid.NewGuid()}/categories";
        var body = new AddMenuCategoryRequestDto("");
        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        raw.Should().Contain("\"status\":400").And.Contain("\"title\":\"MenuCategory\"");
    }

    [Test]
    public async Task AddMenuCategory_WhenMenuNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure<AddMenuCategoryResponse>(Error.NotFound("Menu.InvalidMenuId", "Menu not found")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menus/{Guid.NewGuid()}/categories";
        var body = new AddMenuCategoryRequestDto("Valid Category");
        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AddMenuCategory_WhenMenuBelongsToDifferentRestaurant_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure<AddMenuCategoryResponse>(Error.NotFound("Menu.InvalidMenuId", "Menu not found for this restaurant")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menus/{Guid.NewGuid()}/categories";
        var body = new AddMenuCategoryRequestDto("Valid Category");
        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PUT category details
    
    [Test]
    public async Task UpdateMenuCategoryDetails_WhenValid_Returns204_AndMapsCommand()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var restaurantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<UpdateMenuCategoryDetailsCommand>();
            var cmd = (UpdateMenuCategoryDetailsCommand)req;
            cmd.RestaurantId.Should().Be(restaurantId);
            cmd.MenuCategoryId.Should().Be(categoryId);
            cmd.Name.Should().Be("Updated Appetizers");
            cmd.DisplayOrder.Should().Be(5);
            return Result.Success();
        });
        var body = new UpdateMenuCategoryDetailsRequestDto("Updated Appetizers", 5);
        var path = $"/api/v1/restaurants/{restaurantId}/categories/{categoryId}";
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task UpdateMenuCategoryDetails_WhenNotFound_Returns404()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.NotFound("Menu.CategoryNotFound", "Category not found")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/categories/{Guid.NewGuid()}";
        var body = new UpdateMenuCategoryDetailsRequestDto("Name", 1);
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateMenuCategoryDetails_WhenValidationFails_Returns400()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.Validation("MenuCategory.InvalidDisplayOrder", "Display order must be greater than zero")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/categories/{Guid.NewGuid()}";
        var body = new UpdateMenuCategoryDetailsRequestDto("Valid Name", 0);
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateMenuCategoryDetails_WhenCategoryBelongsToDifferentRestaurant_Returns404()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.NotFound("Menu.CategoryNotFound", "Category not found for this restaurant")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/categories/{Guid.NewGuid()}";
        var body = new UpdateMenuCategoryDetailsRequestDto("Valid Name", 1);
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateMenuCategoryDetails_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/categories/{Guid.NewGuid()}";
        var body = new UpdateMenuCategoryDetailsRequestDto("Name", 1);
        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region DELETE category
    
    [Test]
    public async Task RemoveMenuCategory_WhenValid_Returns204_AndMapsCommand()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var restaurantId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<RemoveMenuCategoryCommand>();
            var cmd = (RemoveMenuCategoryCommand)req;
            cmd.RestaurantId.Should().Be(restaurantId);
            cmd.MenuCategoryId.Should().Be(categoryId);
            return Result.Success();
        });
        var path = $"/api/v1/restaurants/{restaurantId}/categories/{categoryId}";
        var resp = await client.DeleteAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task RemoveMenuCategory_WhenNotFound_Returns404()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.NotFound("Menu.CategoryNotFound", "Category not found")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/categories/{Guid.NewGuid()}";
        var resp = await client.DeleteAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task RemoveMenuCategory_WhenCategoryBelongsToDifferentRestaurant_Returns404()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.NotFound("Menu.CategoryNotFound", "Category not found for this restaurant")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/categories/{Guid.NewGuid()}";
        var resp = await client.DeleteAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task RemoveMenuCategory_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/categories/{Guid.NewGuid()}";
        var resp = await client.DeleteAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task RemoveMenuCategory_WhenCategoryHasMenuItems_Returns409Conflict()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure(Error.Conflict("MenuCategory.HasMenuItems", "Cannot delete category with menu items")));
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/categories/{Guid.NewGuid()}";
        var resp = await client.DeleteAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        raw.Should().Contain("\"status\":409").And.Contain("\"title\":\"MenuCategory\"");
    }

    #endregion
}
