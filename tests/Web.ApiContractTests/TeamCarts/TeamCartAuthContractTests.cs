using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Infrastructure.Serialization.JsonOptions;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.TeamCarts;

public class TeamCartAuthContractTests
{
    #region Authorization Tests - Comprehensive Coverage

    [Test]
    public async Task CreateTeamCart_WithoutAuthHeader_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // No auth header

        var body = new CreateTeamCartCommand(Guid.NewGuid(), "Test Cart", null);
        var path = "/api/v1/team-carts";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine($"Response Headers: {string.Join(", ", resp.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
        TestContext.WriteLine($"Content Headers: {string.Join(", ", resp.Content.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetTeamCartDetails_WithoutAuthHeader_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // No auth header

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}";
        TestContext.WriteLine($"REQUEST GET {path}");

        var resp = await client.GetAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetTeamCartRealTimeViewModel_WithoutAuthHeader_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // No auth header

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/rt";
        TestContext.WriteLine($"REQUEST GET {path}");

        var resp = await client.GetAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task JoinTeamCart_WithoutAuthHeader_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // No auth header

        var body = new { ShareToken = "TOKEN", GuestName = "Guest" };
        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/join";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AddItemToTeamCart_WithoutAuthHeader_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // No auth header

        var body = new
        {
            MenuItemId = Guid.NewGuid(),
            Quantity = 1,
            SelectedCustomizations = (object?)null
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

    [Test]
    public async Task UpdateTeamCartItemQuantity_WithoutAuthHeader_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // No auth header

        var body = new { NewQuantity = 2 };
        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/items/{Guid.NewGuid()}";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST PUT {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PutAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task RemoveItemFromTeamCart_WithoutAuthHeader_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // No auth header

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/items/{Guid.NewGuid()}";
        TestContext.WriteLine($"REQUEST DELETE {path}");

        var resp = await client.DeleteAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task LockTeamCartForPayment_WithoutAuthHeader_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // No auth header

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/lock";
        TestContext.WriteLine($"REQUEST POST {path}");

        var resp = await client.PostAsync(path, null);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ApplyTipToTeamCart_WithoutAuthHeader_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // No auth header

        var body = new { TipAmount = 5.00m };
        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/tip";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ApplyCouponToTeamCart_WithoutAuthHeader_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // No auth header

        var body = new { CouponCode = "SAVE10" };
        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/coupon";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task RemoveCouponFromTeamCart_WithoutAuthHeader_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // No auth header

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/coupon";
        TestContext.WriteLine($"REQUEST DELETE {path}");

        var resp = await client.DeleteAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CommitToCodPayment_WithoutAuthHeader_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // No auth header

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/payments/cod";
        TestContext.WriteLine($"REQUEST POST {path}");

        var resp = await client.PostAsync(path, null);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task InitiateMemberOnlinePayment_WithoutAuthHeader_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // No auth header

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/payments/online";
        TestContext.WriteLine($"REQUEST POST {path}");

        var resp = await client.PostAsync(path, null);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ConvertTeamCartToOrder_WithoutAuthHeader_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // No auth header

        var body = new
        {
            Street = "123 Main St",
            City = "New York",
            State = "NY",
            ZipCode = "10001",
            Country = "USA",
            SpecialInstructions = (string?)null
        };
        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/convert";

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

    #region Valid Auth Header Tests (Spot Checks)

    [Test]
    public async Task CreateTeamCart_WithValidAuthHeader_DoesNotReturn401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "valid-user");

        // Mock a successful response to verify auth passes through
        factory.Sender.RespondWith(_ =>
            YummyZoom.SharedKernel.Result.Success(new YummyZoom.Application.TeamCarts.Commands.CreateTeamCart.CreateTeamCartResponse(
                Guid.NewGuid(), "TOKEN", DateTime.UtcNow.AddHours(1))));

        var body = new CreateTeamCartCommand(Guid.NewGuid(), "Test Cart", null);
        var path = "/api/v1/team-carts";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        // Should NOT be 401 - should be either success or other error (not auth)
        resp.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetTeamCartDetails_WithValidAuthHeader_DoesNotReturn401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "valid-user");

        // Mock a failure response (e.g., not found) to verify auth passes through
        factory.Sender.RespondWith(_ =>
            YummyZoom.SharedKernel.Result.Failure<YummyZoom.Application.TeamCarts.Queries.GetTeamCartDetails.GetTeamCartDetailsResponse>(
                YummyZoom.SharedKernel.Error.NotFound("TeamCart.NotFound", "Not found")));

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}";
        TestContext.WriteLine($"REQUEST GET {path}");

        var resp = await client.GetAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        // Should NOT be 401 - should be 404 in this case since we mocked NotFound
        resp.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound); // Verify the mock worked
    }

    #endregion
}

