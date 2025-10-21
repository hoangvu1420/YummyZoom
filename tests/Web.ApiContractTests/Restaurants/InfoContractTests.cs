using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Application.Restaurants.Queries.GetRestaurantPublicInfo;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

/// <summary>
/// Contract tests for GET /api/v1/restaurants/{restaurantId}/info
/// Tests status codes, DTO shape, and error handling without executing real domain logic.
/// </summary>
public class InfoContractTests
{
    private static RestaurantPublicInfoDto CreateRestaurantInfo(Guid restaurantId, decimal? distanceKm = null)
        => new(
            restaurantId,
            "Test Restaurant",
            "https://example.com/logo.png",
            "https://example.com/background.png",
            "A test restaurant description",
            "Italian",
            new[] { "Italian", "Vegan" },
            true,
            false,
            new AddressDto("123 Main St", "Metro City", "State", "12345", "Country"),
            new ContactInfoDto("123-456-7890", "test@example.com"),
            "Mon-Fri 09:00-17:00",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            distanceKm);

    [Test]
    public async Task GetRestaurantInfo_WhenFound_Returns200WithDtoShape()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // Note: No authentication header needed - this is a public endpoint

        var restaurantId = Guid.NewGuid();
        var expectedDto = CreateRestaurantInfo(restaurantId);

        factory.Sender.RespondWith(req =>
        {
            return req switch
            {
                GetRestaurantPublicInfoQuery q when q.RestaurantId == restaurantId => Result.Success(expectedDto),
                YummyZoom.Application.Reviews.Queries.GetRestaurantReviewSummary.GetRestaurantReviewSummaryQuery q when q.RestaurantId == restaurantId =>
                    Result.Success(new YummyZoom.Application.Reviews.Queries.Common.RestaurantReviewSummaryDto(4.3, 127, 0, 0, 0, 0, 0, 0, DateTime.UtcNow, DateTime.UtcNow)),
                _ => Result.Success(expectedDto)
            };
        });

        var path = $"/api/v1/restaurants/{restaurantId}/info";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        // Assert status and content type
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType?.MediaType.Should().StartWith("application/json");

        // Parse and assert DTO shape with primitive GUID
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        // Assert primitive GUID for restaurantId (not wrapped in value object)
        root.GetProperty("restaurantId").ValueKind.Should().Be(JsonValueKind.String);
        root.GetProperty("restaurantId").GetGuid().Should().Be(restaurantId);

        // Assert other fields
        root.GetProperty("name").GetString().Should().Be("Test Restaurant");
        root.GetProperty("logoUrl").GetString().Should().Be("https://example.com/logo.png");
        root.GetProperty("isAcceptingOrders").GetBoolean().Should().BeTrue();
        root.GetProperty("city").GetString().Should().Be("Metro City");

        // Assert cuisineTags is an array
        var cuisineTags = root.GetProperty("cuisineTags");
        cuisineTags.ValueKind.Should().Be(JsonValueKind.Array);
        cuisineTags.GetArrayLength().Should().Be(2);
        cuisineTags[0].GetString().Should().Be("Italian");
        cuisineTags[1].GetString().Should().Be("Vegan");

        // Optional rating fields exist and are nullable
        root.TryGetProperty("avgRating", out var avgProp).Should().BeTrue();
        avgProp.ValueKind.Should().BeOneOf(JsonValueKind.Null, JsonValueKind.Number);
        root.TryGetProperty("ratingCount", out var countProp).Should().BeTrue();
        countProp.ValueKind.Should().BeOneOf(JsonValueKind.Null, JsonValueKind.Number);

        // Optional distance field exists and should be null when lat/lng not provided
        root.TryGetProperty("distanceKm", out var distanceProp).Should().BeTrue();
        distanceProp.ValueKind.Should().Be(JsonValueKind.Null);

        // Verify a request was sent for this restaurant (either info or summary)
        factory.Sender.LastRequest.Should().NotBeNull();
    }

    [Test]
    public async Task GetRestaurantInfo_WhenSummaryExists_PopulatesRatingFields()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var restaurantId = Guid.NewGuid();
        var expectedDto = CreateRestaurantInfo(restaurantId);

        factory.Sender.RespondWith(req =>
        {
            return req switch
            {
                GetRestaurantPublicInfoQuery q when q.RestaurantId == restaurantId =>
                    Result.Success(expectedDto),
                YummyZoom.Application.Reviews.Queries.GetRestaurantReviewSummary.GetRestaurantReviewSummaryQuery q when q.RestaurantId == restaurantId =>
                    Result.Success(new YummyZoom.Application.Reviews.Queries.Common.RestaurantReviewSummaryDto(4.6, 127, 1, 2, 3, 4, 117, 80, DateTime.UtcNow, DateTime.UtcNow)),
                _ => Result.Success(expectedDto)
            };
        });

        var path = $"/api/v1/restaurants/{restaurantId}/info";
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        root.GetProperty("avgRating").GetDecimal().Should().BeGreaterThan(0);
        root.GetProperty("ratingCount").GetInt32().Should().Be(127);
    }

    [Test]
    public async Task GetRestaurantInfo_WhenNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        factory.Sender.RespondWith(_ =>
            Result.Failure<RestaurantPublicInfoDto>(Error.NotFound("Public.GetRestaurantPublicInfo.NotFound", "Restaurant info was not found.")));

        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/info";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var prob = JsonSerializer.Deserialize<ProblemDetails>(raw);
        prob!.Status.Should().Be(404);
        prob.Title!.StartsWith("Public").Should().BeTrue();
    }

    [Test]
    public async Task GetRestaurantInfo_WhenLatLngProvided_ReturnsDistanceKm()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var restaurantId = Guid.NewGuid();
        var expectedDto = CreateRestaurantInfo(restaurantId, distanceKm: 2.5m);

        factory.Sender.RespondWith(req =>
        {
            return req switch
            {
                GetRestaurantPublicInfoQuery q when q.RestaurantId == restaurantId && q.Lat.HasValue && q.Lng.HasValue =>
                    Result.Success(expectedDto),
                YummyZoom.Application.Reviews.Queries.GetRestaurantReviewSummary.GetRestaurantReviewSummaryQuery q when q.RestaurantId == restaurantId =>
                    Result.Success(new YummyZoom.Application.Reviews.Queries.Common.RestaurantReviewSummaryDto(4.3, 127, 0, 0, 0, 0, 0, 0, DateTime.UtcNow, DateTime.UtcNow)),
                _ => Result.Success(expectedDto)
            };
        });

        var path = $"/api/v1/restaurants/{restaurantId}/info?lat=37.7749&lng=-122.4194";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        // Distance field should be present and have a numeric value
        root.TryGetProperty("distanceKm", out var distanceProp).Should().BeTrue();
        distanceProp.ValueKind.Should().Be(JsonValueKind.Number);
        distanceProp.GetDecimal().Should().Be(2.5m);

        // Note: We don't verify query parameters here because the endpoint makes multiple requests
        // (GetRestaurantPublicInfoQuery + GetRestaurantReviewSummaryQuery) and LastRequest will be
        // the review summary. Contract tests focus on HTTP API shape, not internal request flow.
    }

    [Test]
    public async Task GetRestaurantInfo_WhenLatLngNotProvided_ReturnsNullDistance()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var restaurantId = Guid.NewGuid();
        var expectedDto = CreateRestaurantInfo(restaurantId, distanceKm: null);

        factory.Sender.RespondWith(req =>
        {
            return req switch
            {
                GetRestaurantPublicInfoQuery q when q.RestaurantId == restaurantId =>
                    Result.Success(expectedDto),
                YummyZoom.Application.Reviews.Queries.GetRestaurantReviewSummary.GetRestaurantReviewSummaryQuery q when q.RestaurantId == restaurantId =>
                    Result.Success(new YummyZoom.Application.Reviews.Queries.Common.RestaurantReviewSummaryDto(4.3, 127, 0, 0, 0, 0, 0, 0, DateTime.UtcNow, DateTime.UtcNow)),
                _ => Result.Success(expectedDto)
            };
        });

        var path = $"/api/v1/restaurants/{restaurantId}/info";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        // Distance field should be null when lat/lng not provided
        root.TryGetProperty("distanceKm", out var distanceProp).Should().BeTrue();
        distanceProp.ValueKind.Should().Be(JsonValueKind.Null);

        // Note: We don't verify query parameters here because the endpoint makes multiple requests
        // and LastRequest will be the review summary, not the info query. Contract tests focus on
        // HTTP API shape, not internal request flow.
    }

    // Note: Validation tests are better suited for functional tests since contract tests use a fake
    // sender that bypasses the MediatR pipeline (including validation behavior). 
    // See Application.FunctionalTests for proper validation testing.
}
