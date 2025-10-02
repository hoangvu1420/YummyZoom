using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Infrastructure.Serialization.JsonOptions;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.Web.Services;

namespace YummyZoom.Web.ApiContractTests.TeamCarts;

/// <summary>
/// Specialized factory for testing feature availability with custom service configuration
/// </summary>
public class TeamCartFeatureAvailabilityWebAppFactory : ApiContractWebAppFactory
{
    private readonly ITeamCartFeatureAvailability? _mockAvailability;

    public TeamCartFeatureAvailabilityWebAppFactory(ITeamCartFeatureAvailability? mockAvailability = null)
    {
        _mockAvailability = mockAvailability;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        if (_mockAvailability != null)
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove existing ITeamCartFeatureAvailability registration
                var existing = services.FirstOrDefault(d => d.ServiceType == typeof(ITeamCartFeatureAvailability));
                if (existing != null) services.Remove(existing);

                // Add mock implementation
                services.AddSingleton(_mockAvailability);
            });
        }
    }
}

/// <summary>
/// Helper class to create feature availability mocks
/// </summary>
public static class FeatureAvailabilityMocks
{
    public static ITeamCartFeatureAvailability CreateDisabled()
    {
        var mock = new Mock<ITeamCartFeatureAvailability>();
        mock.Setup(x => x.Enabled).Returns(false);
        mock.Setup(x => x.RealTimeReady).Returns(false);
        return mock.Object;
    }

    public static ITeamCartFeatureAvailability CreateRealTimeNotReady()
    {
        var mock = new Mock<ITeamCartFeatureAvailability>();
        mock.Setup(x => x.Enabled).Returns(true);
        mock.Setup(x => x.RealTimeReady).Returns(false);
        return mock.Object;
    }

    public static ITeamCartFeatureAvailability CreateAvailable()
    {
        var mock = new Mock<ITeamCartFeatureAvailability>();
        mock.Setup(x => x.Enabled).Returns(true);
        mock.Setup(x => x.RealTimeReady).Returns(true);
        return mock.Object;
    }
}

public class TeamCartFeatureAvailabilityContractTests
{
    #region Feature Disabled Tests

    [Test]
    public async Task CreateTeamCart_WhenFeatureDisabled_Returns503()
    {
        // Mock ITeamCartFeatureAvailability to return disabled
        var mockAvailability = FeatureAvailabilityMocks.CreateDisabled();
        var factory = new TeamCartFeatureAvailabilityWebAppFactory(mockAvailability);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var body = new CreateTeamCartCommand(Guid.NewGuid(), "Test Cart", null);
        var path = "/api/v1/team-carts";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path} (Feature Disabled)");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task GetTeamCartDetails_WhenFeatureDisabled_Returns503()
    {
        // Mock ITeamCartFeatureAvailability to return disabled
        var mockAvailability = FeatureAvailabilityMocks.CreateDisabled();
        var factory = new TeamCartFeatureAvailabilityWebAppFactory(mockAvailability);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}";
        TestContext.WriteLine($"REQUEST GET {path} (Feature Disabled)");

        var resp = await client.GetAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task GetTeamCartRealTimeViewModel_WhenFeatureDisabled_Returns503()
    {
        // Mock ITeamCartFeatureAvailability to return disabled
        var mockAvailability = FeatureAvailabilityMocks.CreateDisabled();
        var factory = new TeamCartFeatureAvailabilityWebAppFactory(mockAvailability);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/rt";
        TestContext.WriteLine($"REQUEST GET {path} (Feature Disabled)");

        var resp = await client.GetAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task JoinTeamCart_WhenFeatureDisabled_Returns503()
    {
        var mockAvailability = FeatureAvailabilityMocks.CreateDisabled();
        var factory = new TeamCartFeatureAvailabilityWebAppFactory(mockAvailability);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-2");

        var body = new { ShareToken = "TOKEN", GuestName = "Guest" };
        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/join";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path} (Feature Disabled)");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task ConvertTeamCartToOrder_WhenFeatureDisabled_Returns503()
    {
        var mockAvailability = FeatureAvailabilityMocks.CreateDisabled();
        var factory = new TeamCartFeatureAvailabilityWebAppFactory(mockAvailability);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "host-user");

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
        TestContext.WriteLine($"REQUEST POST {path} (Feature Disabled)");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    #endregion

    #region RealTime Not Ready Tests

    [Test]
    public async Task CreateTeamCart_WhenRealTimeNotReady_Returns503()
    {
        var mockAvailability = FeatureAvailabilityMocks.CreateRealTimeNotReady();
        var factory = new TeamCartFeatureAvailabilityWebAppFactory(mockAvailability);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var body = new CreateTeamCartCommand(Guid.NewGuid(), "Test Cart", null);
        var path = "/api/v1/team-carts";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path} (RealTime Not Ready)");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task GetTeamCartRealTimeViewModel_WhenRealTimeNotReady_Returns503()
    {
        var mockAvailability = FeatureAvailabilityMocks.CreateRealTimeNotReady();
        var factory = new TeamCartFeatureAvailabilityWebAppFactory(mockAvailability);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/rt";
        TestContext.WriteLine($"REQUEST GET {path} (RealTime Not Ready)");

        var resp = await client.GetAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    #endregion

    #region Feature Available Tests

    [Test]
    public async Task CreateTeamCart_WhenFeatureAvailable_DoesNotReturn503()
    {
        var mockAvailability = FeatureAvailabilityMocks.CreateAvailable();
        var factory = new TeamCartFeatureAvailabilityWebAppFactory(mockAvailability);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        // Mock a successful response 
        factory.Sender.RespondWith(_ =>
            YummyZoom.SharedKernel.Result.Success(new YummyZoom.Application.TeamCarts.Commands.CreateTeamCart.CreateTeamCartResponse(
                Guid.NewGuid(), "TOKEN", DateTime.UtcNow.AddHours(1))));

        var body = new CreateTeamCartCommand(Guid.NewGuid(), "Test Cart", null);
        var path = "/api/v1/team-carts";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path} (Feature Available)");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        // Should NOT be 503 - should process the request and return success or other business error
        resp.StatusCode.Should().NotBe(HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task GetTeamCartRealTimeViewModel_WhenFeatureAvailable_DoesNotReturn503()
    {
        var mockAvailability = FeatureAvailabilityMocks.CreateAvailable();
        var factory = new TeamCartFeatureAvailabilityWebAppFactory(mockAvailability);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        // Mock a not found response to verify request processing
        factory.Sender.RespondWith(_ =>
            YummyZoom.SharedKernel.Result.Failure<YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel.GetTeamCartRealTimeViewModelResponse>(
                YummyZoom.SharedKernel.Error.NotFound("TeamCart.NotFound", "Not found")));

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/rt";
        TestContext.WriteLine($"REQUEST GET {path} (Feature Available)");

        var resp = await client.GetAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        // Should NOT be 503 - should process and return 404 in this case
        resp.StatusCode.Should().NotBe(HttpStatusCode.ServiceUnavailable);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound); // Verify the mock worked
    }

    #endregion

    #region Spot Check Other Endpoints

    [Test]
    public async Task AddItemToTeamCart_WhenFeatureDisabled_Returns503()
    {
        var mockAvailability = FeatureAvailabilityMocks.CreateDisabled();
        var factory = new TeamCartFeatureAvailabilityWebAppFactory(mockAvailability);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var body = new
        {
            MenuItemId = Guid.NewGuid(),
            Quantity = 1,
            SelectedCustomizations = (object?)null
        };
        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/items";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path} (Feature Disabled)");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task InitiateMemberOnlinePayment_WhenFeatureDisabled_Returns503()
    {
        var mockAvailability = FeatureAvailabilityMocks.CreateDisabled();
        var factory = new TeamCartFeatureAvailabilityWebAppFactory(mockAvailability);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "member-user");

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/payments/online";
        TestContext.WriteLine($"REQUEST POST {path} (Feature Disabled)");

        var resp = await client.PostAsync(path, null);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    #endregion
}

