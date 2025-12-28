using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.RemoveItemFromTeamCart;
using YummyZoom.Application.TeamCarts.Commands.UpdateTeamCartItemQuantity;
using YummyZoom.Infrastructure.Serialization.JsonOptions;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.TeamCarts;

public class TeamCartItemsContractTests
{
    #region Add Item Tests

    [Test]
    public async Task AddItemToTeamCart_WithValidRequest_Returns204()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var cartId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<AddItemToTeamCartCommand>();
            var cmd = (AddItemToTeamCartCommand)req;
            cmd.TeamCartId.Should().Be(cartId);
            cmd.MenuItemId.Should().Be(menuItemId);
            cmd.Quantity.Should().Be(2);
            cmd.SelectedCustomizations.Should().NotBeNull();
            return Result.Success();
        });

        var customizations = new List<AddItemToTeamCartCustomizationSelection>
        {
            new(Guid.NewGuid(), Guid.NewGuid()),
            new(Guid.NewGuid(), Guid.NewGuid())
        };

        var body = new
        {
            MenuItemId = menuItemId,
            Quantity = 2,
            SelectedCustomizations = customizations
        };

        var path = $"/api/v1/team-carts/{cartId}/items";
        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task AddItemToTeamCart_WithInvalidMenuItem_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure(
            Error.NotFound("MenuItem.NotFound", "Menu item not found")));

        var body = new
        {
            MenuItemId = Guid.NewGuid(),
            Quantity = 1,
            SelectedCustomizations = (List<AddItemToTeamCartCustomizationSelection>?)null
        };

        var path = $"/api/v1/team-carts/{cartId}/items";
        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(404);
        problem.Title.Should().Be("MenuItem.NotFound");
    }

    [Test]
    public async Task AddItemToTeamCart_WithInvalidQuantity_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure(
            Error.Validation("AddItemToTeamCart.InvalidQuantity", "Quantity must be positive")));

        var body = new
        {
            MenuItemId = Guid.NewGuid(),
            Quantity = 0,
            SelectedCustomizations = (List<AddItemToTeamCartCustomizationSelection>?)null
        };

        var path = $"/api/v1/team-carts/{cartId}/items";
        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(400);
        problem.Title.Should().Be("AddItemToTeamCart.InvalidQuantity");
    }

    [Test]
    public async Task AddItemToTeamCart_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var body = new
        {
            MenuItemId = Guid.NewGuid(),
            Quantity = 1,
            SelectedCustomizations = (List<AddItemToTeamCartCustomizationSelection>?)null
        };

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/items";
        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Update Item Quantity Tests

    [Test]
    public async Task UpdateTeamCartItemQuantity_WithValidRequest_Returns204()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<UpdateTeamCartItemQuantityCommand>();
            var cmd = (UpdateTeamCartItemQuantityCommand)req;
            cmd.TeamCartId.Should().Be(cartId);
            cmd.TeamCartItemId.Should().Be(itemId);
            cmd.NewQuantity.Should().Be(3);
            return Result.Success();
        });

        var body = new { NewQuantity = 3 };
        var path = $"/api/v1/team-carts/{cartId}/items/{itemId}";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST PUT {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task UpdateTeamCartItemQuantity_WithZeroQuantity_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure(
            Error.Validation("UpdateTeamCartItemQuantity.InvalidQuantity", "Quantity must be positive")));

        var body = new { NewQuantity = 0 };
        var path = $"/api/v1/team-carts/{cartId}/items/{itemId}";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST PUT {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(400);
        problem.Title.Should().Be("UpdateTeamCartItemQuantity.InvalidQuantity");
    }

    [Test]
    public async Task UpdateTeamCartItemQuantity_WhenItemNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        // Add a simple responder to see if we even reach MediatR
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<UpdateTeamCartItemQuantityCommand>();
            var cmd = (UpdateTeamCartItemQuantityCommand)req;
            cmd.TeamCartId.Should().Be(cartId);
            cmd.TeamCartItemId.Should().Be(itemId);
            cmd.NewQuantity.Should().Be(2);
            return Result.Failure(Error.NotFound("TeamCartItem.NotFound", "Item not found in cart"));
        });

        var body = new { NewQuantity = 2 };
        var path = $"/api/v1/team-carts/{cartId}/items/{itemId}";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST PUT {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(404);
        problem.Title.Should().Be("TeamCartItem.NotFound");
    }

    #endregion

    #region Remove Item Tests

    [Test]
    public async Task RemoveItemFromTeamCart_WithValidRequest_Returns204()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<RemoveItemFromTeamCartCommand>();
            var cmd = (RemoveItemFromTeamCartCommand)req;
            cmd.TeamCartId.Should().Be(cartId);
            cmd.TeamCartItemId.Should().Be(itemId);
            return Result.Success();
        });

        var path = $"/api/v1/team-carts/{cartId}/items/{itemId}";
        TestContext.WriteLine($"REQUEST DELETE {path}");

        var resp = await client.DeleteAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task RemoveItemFromTeamCart_WhenItemNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure(
            Error.NotFound("TeamCartItem.NotFound", "Item not found in cart")));

        var path = $"/api/v1/team-carts/{cartId}/items/{itemId}";
        TestContext.WriteLine($"REQUEST DELETE {path}");

        var resp = await client.DeleteAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(404);
        problem.Title.Should().Be("TeamCartItem.NotFound");
    }

    [Test]
    public async Task RemoveItemFromTeamCart_WhenNotOwner_Returns403Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-2");

        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure(
            Error.Failure("TeamCartItem.NotOwner", "Only the item owner can remove this item")));

        var path = $"/api/v1/team-carts/{cartId}/items/{itemId}";
        TestContext.WriteLine($"REQUEST DELETE {path}");

        var resp = await client.DeleteAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(400);
        problem.Title.Should().Be("TeamCartItem.NotOwner");
    }

    [Test]
    public async Task RemoveItemFromTeamCart_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/items/{Guid.NewGuid()}";
        TestContext.WriteLine($"REQUEST DELETE {path}");

        var resp = await client.DeleteAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
