using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Application.Restaurants.Queries.GetRestaurantAggregatedDetails;
using YummyZoom.Application.Reviews.Queries.Common;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

[TestFixture]
public class AggregatedDetailsContractTests
{
    private static RestaurantAggregatedDetailsDto CreateDetails(
        Guid restaurantId,
        DateTimeOffset? lastChanged = null,
        DateTimeOffset? menuRebuiltAt = null,
        DateTime? summaryUpdatedAtUtc = null)
    {
        var info = new RestaurantPublicInfoDto(
            restaurantId,
            "Bella Vista",
            "https://example.com/logo.png",
            "https://example.com/cover.png",
            "Cozy Italian spot",
            "Italian",
            new[] { "Italian", "Pasta" },
            true,
            true,
            new AddressDto("1 Main St", "Metro City", "CA", "90000", "USA"),
            new ContactInfoDto("555-0000", "hello@example.com"),
            "Mon-Sun 11:00-22:00",
            DateTimeOffset.UtcNow.AddYears(-5),
            DateTimeOffset.UtcNow.AddMinutes(-20),
            1.2m);

        var menu = new RestaurantAggregatedMenuDto(
            """
            {"menus":[{"id":"m1","name":"Dinner","categories":[{"id":"c1","name":"Starters"}]}]}
            """,
            menuRebuiltAt ?? DateTimeOffset.UtcNow.AddMinutes(-15));

        var summaryUpdatedUtc = summaryUpdatedAtUtc ?? DateTime.UtcNow.AddMinutes(-10);
        var summary = new RestaurantReviewSummaryDto(
            AverageRating: 4.5,
            TotalReviews: 278,
            Ratings1: 5,
            Ratings2: 8,
            Ratings3: 25,
            Ratings4: 80,
            Ratings5: 160,
            TotalWithText: 140,
            LastReviewAtUtc: summaryUpdatedUtc.AddMinutes(-5),
            UpdatedAtUtc: summaryUpdatedUtc);

        var changed = lastChanged ?? DateTimeOffset.UtcNow.AddMinutes(-8);

        return new RestaurantAggregatedDetailsDto(info, menu, summary, changed);
    }

    [Test]
    public async Task GetAggregatedDetails_WhenFound_ReturnsAggregatedPayload()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var restaurantId = Guid.NewGuid();
        var details = CreateDetails(restaurantId);

        factory.Sender.RespondWith(req => req switch
        {
            GetRestaurantAggregatedDetailsQuery q when q.RestaurantId == restaurantId => Result.Success(details),
            _ => throw new AssertionException("Unexpected request")
        });

        var path = $"/api/v1/restaurants/{restaurantId}/details";
        TestContext.WriteLine($"REQUEST GET {path}");
        var response = await client.GetAsync(path);
        var raw = await response.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)response.StatusCode} {response.StatusCode}\n{raw}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag.Should().NotBeNull();
        response.Headers.CacheControl?.Public.Should().BeTrue();
        response.Headers.CacheControl?.MaxAge.Should().Be(TimeSpan.FromMinutes(2));

        using var document = JsonDocument.Parse(raw);
        var root = document.RootElement;

        var info = root.GetProperty("info");
        info.GetProperty("restaurantId").GetGuid().Should().Be(restaurantId);
        info.GetProperty("name").GetString().Should().Be("Bella Vista");
        info.GetProperty("avgRating").GetDecimal().Should().BeGreaterOrEqualTo(0);
        info.GetProperty("distanceKm").GetDecimal().Should().Be(1.2m);

        var menu = root.GetProperty("menu");
        menu.GetProperty("lastRebuiltAt").GetDateTimeOffset().Should().Be(details.Menu.LastRebuiltAt);
        menu.GetProperty("data").TryGetProperty("menus", out var menusArray).Should().BeTrue();
        menusArray.ValueKind.Should().Be(JsonValueKind.Array);
        menusArray.GetArrayLength().Should().Be(1);

        var summary = root.GetProperty("reviewSummary");
        summary.GetProperty("totalReviews").GetInt32().Should().Be(278);
        summary.GetProperty("averageRating").GetDouble().Should().BeGreaterThan(0);

        root.GetProperty("lastChangedUtc").GetDateTimeOffset().Should().Be(details.LastChangedUtc);
    }

    [Test]
    public async Task GetAggregatedDetails_WithMatchingEtag_Returns304()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var restaurantId = Guid.NewGuid();
        var lastChanged = DateTimeOffset.UtcNow.AddMinutes(-3);
        var details = CreateDetails(restaurantId, lastChanged: lastChanged);
        var etag = BuildWeakEtag(restaurantId, lastChanged);

        factory.Sender.RespondWith(req => req switch
        {
            GetRestaurantAggregatedDetailsQuery q when q.RestaurantId == restaurantId => Result.Success(details),
            _ => throw new AssertionException("Unexpected request")
        });

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/restaurants/{restaurantId}/details");
        request.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Parse(etag));
        request.Headers.IfModifiedSince = lastChanged.UtcDateTime;

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
        response.Headers.CacheControl?.Public.Should().BeTrue();
        response.Headers.CacheControl?.MaxAge.Should().Be(TimeSpan.FromMinutes(2));
    }

    [Test]
    public async Task GetAggregatedDetails_WithLatLng_DisablesCaching()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var restaurantId = Guid.NewGuid();
        var details = CreateDetails(restaurantId);

        factory.Sender.RespondWith(req => req switch
        {
            GetRestaurantAggregatedDetailsQuery q when q.RestaurantId == restaurantId => Result.Success(details),
            _ => throw new AssertionException("Unexpected request")
        });

        var response = await client.GetAsync($"/api/v1/restaurants/{restaurantId}/details?lat=37.0&lng=-122.0");
        var raw = await response.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)response.StatusCode} {response.StatusCode}\n{raw}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag.Should().BeNull();
        response.Headers.CacheControl?.NoStore.Should().BeTrue();
    }

    [Test]
    public async Task GetAggregatedDetails_WhenNotFound_Returns404()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var restaurantId = Guid.NewGuid();

        factory.Sender.RespondWith(req => req switch
        {
            GetRestaurantAggregatedDetailsQuery q when q.RestaurantId == restaurantId =>
                Result.Failure<RestaurantAggregatedDetailsDto>(GetRestaurantAggregatedDetailsErrors.NotFound(restaurantId)),
            _ => throw new AssertionException("Unexpected request")
        });

        var response = await client.GetAsync($"/api/v1/restaurants/{restaurantId}/details");
        var raw = await response.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)response.StatusCode} {response.StatusCode}\n{raw}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static string BuildWeakEtag(Guid restaurantId, DateTimeOffset lastChangedUtc)
        => $"W/\"r:{restaurantId}:t:{lastChangedUtc.UtcTicks}\"";
}
