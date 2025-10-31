using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.Restaurants.Queries.Public.GetMenuItemDetails;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

[TestFixture]
public class MenuItemPublicDetailsContractTests
{
    private static MenuItemPublicDetailsDto CreateDto(Guid restaurantId, Guid itemId, DateTimeOffset? lastModified = null)
    {
        var groups = new List<CustomizationGroupDto>
        {
            new(
                GroupId: Guid.NewGuid(),
                Name: "Gọi thêm",
                Type: "multi",
                Required: false,
                Min: 0,
                Max: 3,
                Items: new List<CustomizationChoiceDto>
                {
                    new(Guid.NewGuid(), "Extra A", 10000m, false, false),
                    new(Guid.NewGuid(), "Extra B", 5000m, true, false)
                })
        };

        return new MenuItemPublicDetailsDto(
            RestaurantId: restaurantId,
            ItemId: itemId,
            Name: "Bún đậu",
            Description: "Ngon",
            ImageUrl: "https://img",
            BasePrice: 45000m,
            Currency: "VND",
            IsAvailable: true,
            SoldCount: 1234,
            Rating: 4.6,
            ReviewCount: 120,
            CustomizationGroups: groups,
            NotesHint: "Hint",
            Limits: new ItemQuantityLimits(1, 99),
            Upsell: new List<UpsellSuggestionDto> { new(ItemId: Guid.NewGuid(), Name: "Nem rán", Price: 10000m, ImageUrl: "https://img2") },
            LastModified: lastModified ?? DateTimeOffset.UtcNow.AddMinutes(-5));
    }

    [Test]
    public async Task GetPublicDetails_WhenFound_ReturnsPayload_WithCachingHeaders()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var restaurantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var dto = CreateDto(restaurantId, itemId);

        factory.Sender.RespondWith(req => req switch
        {
            GetMenuItemPublicDetailsQuery q when q.RestaurantId == restaurantId && q.ItemId == itemId => Result.Success(dto),
            _ => throw new AssertionException("Unexpected request")
        });

        var path = $"/api/v1/restaurants/{restaurantId}/menu-items/{itemId}";
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Headers.ETag.Should().NotBeNull();
        resp.Headers.CacheControl?.Public.Should().BeTrue();
        resp.Headers.CacheControl?.MaxAge.Should().Be(TimeSpan.FromMinutes(2));

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        root.GetProperty("restaurantId").GetGuid().Should().Be(restaurantId);
        root.GetProperty("itemId").GetGuid().Should().Be(itemId);
        root.GetProperty("name").GetString().Should().Be("Bún đậu");
        root.GetProperty("currency").GetString().Should().Be("VND");
        root.GetProperty("basePrice").GetDecimal().Should().Be(45000m);
        var groups = root.GetProperty("customizationGroups");
        groups.ValueKind.Should().Be(JsonValueKind.Array);
        groups.GetArrayLength().Should().BeGreaterThan(0);
        var limits = root.GetProperty("limits");
        limits.GetProperty("minQty").GetInt32().Should().Be(1);
        limits.GetProperty("maxQty").GetInt32().Should().Be(99);
    }

    [Test]
    public async Task GetPublicDetails_WithMatchingEtag_Returns304()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var restaurantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var last = DateTimeOffset.UtcNow.AddMinutes(-10);
        var dto = CreateDto(restaurantId, itemId, last);
        var etag = $"W/\"r:{restaurantId}:t:{last.UtcTicks}\"";

        factory.Sender.RespondWith(req => req switch
        {
            GetMenuItemPublicDetailsQuery q when q.RestaurantId == restaurantId && q.ItemId == itemId => Result.Success(dto),
            _ => throw new AssertionException("Unexpected request")
        });

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/restaurants/{restaurantId}/menu-items/{itemId}");
        request.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Parse(etag));
        request.Headers.IfModifiedSince = last.UtcDateTime;
        var resp = await client.SendAsync(request);

        resp.StatusCode.Should().Be(HttpStatusCode.NotModified);
        resp.Headers.CacheControl?.Public.Should().BeTrue();
        resp.Headers.CacheControl?.MaxAge.Should().Be(TimeSpan.FromMinutes(2));
    }

    [Test]
    public async Task GetPublicDetails_WhenNotFound_Returns404()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var restaurantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        factory.Sender.RespondWith(req => req switch
        {
            GetMenuItemPublicDetailsQuery q when q.RestaurantId == restaurantId && q.ItemId == itemId =>
                Result.Failure<MenuItemPublicDetailsDto>(Error.NotFound("Public.MenuItem.NotFound", "Missing")),
            _ => throw new AssertionException("Unexpected request")
        });

        var resp = await client.GetAsync($"/api/v1/restaurants/{restaurantId}/menu-items/{itemId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
