using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.MenuItems.Commands.CreateMenuItem;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Application.Restaurants.Queries.GetFullMenu;
using YummyZoom.Infrastructure.Persistence.ReadModels.FullMenu;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public class GetFullMenuQueryTests : BaseTestFixture
{
    [Test]
    public async Task Caching_InvalidateOnMenuUpsert_ReturnsFreshData()
    {
        await ResetState();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        // Ensure FullMenuView exists
        using (var scope = CreateScope())
        {
            var maint = scope.ServiceProvider.GetRequiredService<IFullMenuViewMaintainer>();
            var rebuilt = await maint.RebuildAsync(restaurantId);
            await maint.UpsertAsync(restaurantId, rebuilt.menuJson, rebuilt.lastRebuiltAt);
        }

        // Warm cache
        var first = await SendAndUnwrapAsync(new GetFullMenuQuery(restaurantId));
        var baseline = first.LastRebuiltAt;
        var again = await SendAndUnwrapAsync(new GetFullMenuQuery(restaurantId));
        again.LastRebuiltAt.Should().Be(baseline); // served from cache

        // Create a new menu item to trigger rebuild + invalidation
        await RunAsRestaurantStaffAsync("staff@restaurant.com", restaurantId);
        var menuCategoryId = Testing.TestData.GetMenuCategoryId("Main Dishes");
        var cmd = new CreateMenuItemCommand(
            RestaurantId: restaurantId,
            MenuCategoryId: menuCategoryId,
            Name: $"CacheInvalidate-{Guid.NewGuid():N}",
            Description: "Caching test item",
            Price: 12.34m,
            Currency: "USD",
            ImageUrl: null,
            IsAvailable: true,
            DietaryTagIds: null);

        var createResult = await SendAsync(cmd);
        createResult.ShouldBeSuccessful();

        // Process outbox to run handlers and publish invalidation
        await DrainOutboxAsync();

        // Poll a few times to allow subscriber to process pub/sub
        GetFullMenuResponse updated = again;
        var attempts = 0;
        while (attempts++ < 20)
        {
            updated = await SendAndUnwrapAsync(new GetFullMenuQuery(restaurantId));
            if (updated.LastRebuiltAt > baseline)
                break;
            await Task.Delay(100);
        }

        updated.LastRebuiltAt.Should().BeAfter(baseline, "cache should be invalidated and view rebuilt time should advance");
    }

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
