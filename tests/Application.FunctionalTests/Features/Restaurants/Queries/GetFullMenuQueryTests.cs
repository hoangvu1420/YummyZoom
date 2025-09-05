using System.Text.Json;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Restaurants.Queries.GetFullMenu;
using YummyZoom.Infrastructure.Data.Models;
using YummyZoom.Infrastructure.Data.ReadModels.FullMenu;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public class GetFullMenuQueryTests : BaseTestFixture
{
    [Test]
    public async Task Success_ReturnsSeededValues_WithoutAlteringJsonOrOffset()
    {
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var prettyJson = "{\n  \"version\": 1,\n  \"items\": [ \"test string\" ]\n}";
        var offsetTime = new DateTimeOffset(2024, 12, 25, 03, 30, 15, TimeSpan.Zero); // UTC

        await AddAsync(new FullMenuView
        {
            RestaurantId = restaurantId,
            MenuJson = prettyJson,
            LastRebuiltAt = offsetTime
        });

        var result = await SendAsync(new GetFullMenuQuery(restaurantId));

        result.ShouldBeSuccessful();
        // JSONB canonicalizes formatting and may reorder properties; verify semantic content
        using var doc = JsonDocument.Parse(result.Value.MenuJson);
        var root = doc.RootElement;
        root.TryGetProperty("version", out var versionProp).Should().BeTrue();
        versionProp.GetInt32().Should().Be(1);
        root.TryGetProperty("items", out var itemsProp).Should().BeTrue();
        itemsProp.ValueKind.Should().Be(JsonValueKind.Array);
        itemsProp.GetArrayLength().Should().Be(1);
        itemsProp[0].GetString().Should().Be("test string");
        result.Value.LastRebuiltAt.Should().Be(offsetTime);
    }

    [Test]
    public async Task Success_MultiRowSafety_ReturnsOnlyRequestedRestaurant()
    {
        var restaurantA = Testing.TestData.DefaultRestaurantId;
        var restaurantB = Guid.NewGuid();

        await AddAsync(new FullMenuView
        {
            RestaurantId = restaurantA,
            MenuJson = "{\"version\":1,\"a\":true}",
            LastRebuiltAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        });
        await AddAsync(new FullMenuView
        {
            RestaurantId = restaurantB,
            MenuJson = "{\"version\":1,\"b\":true}",
            LastRebuiltAt = DateTimeOffset.UtcNow.AddMinutes(-3)
        });

        var result = await SendAsync(new GetFullMenuQuery(restaurantA));

        result.ShouldBeSuccessful();
        using var doc = JsonDocument.Parse(result.Value.MenuJson);
        var root = doc.RootElement;
        root.TryGetProperty("a", out var aProp).Should().BeTrue();
        aProp.ValueKind.Should().Be(JsonValueKind.True);
        root.TryGetProperty("b", out _).Should().BeFalse();
    }

    [Test]
    public async Task EmptyContent_CurrentBehavior_AllowsEmptyJson()
    {
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        await AddAsync(new FullMenuView
        {
            RestaurantId = restaurantId,
            MenuJson = "[]",
            LastRebuiltAt = DateTimeOffset.UtcNow
        });

        var result = await SendAsync(new GetFullMenuQuery(restaurantId));

        result.ShouldBeSuccessful();
        result.Value.MenuJson.Should().Be("[]");
    }

    [Test]
    public async Task NotFound_WhenRowMissing()
    {
        var result = await SendAsync(new GetFullMenuQuery(Guid.NewGuid()));
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(GetFullMenuErrors.NotFound);
    }

    [Test]
    public async Task Validation_EmptyRestaurantId_ShouldThrow()
    {
        var act = async () => await SendAsync(new GetFullMenuQuery(Guid.Empty));
        await act.Should().ThrowAsync<ValidationException>();
    }
}


