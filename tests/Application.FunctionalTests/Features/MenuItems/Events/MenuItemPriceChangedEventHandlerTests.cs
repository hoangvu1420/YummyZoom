using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.MenuItems.Commands.CreateMenuItem;
using YummyZoom.Application.MenuItems.Commands.UpdateMenuItemDetails;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Models;
using YummyZoom.Infrastructure.Data.ReadModels.FullMenu;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Events;

using static Testing;

public class MenuItemPriceChangedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task UpdatePrice_ShouldRebuildFullMenuView_AndBeIdempotent()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = TestData.DefaultRestaurantId;
        var categoryId = TestData.GetMenuCategoryId("Main Dishes");

        var create = new CreateMenuItemCommand(
            RestaurantId: restaurantId,
            MenuCategoryId: categoryId,
            Name: $"Price-{Guid.NewGuid():N}",
            Description: "Before price",
            Price: 10m,
            Currency: "USD",
            ImageUrl: null,
            IsAvailable: true,
            DietaryTagIds: null);
        var createResult = await SendAsync(create);
        createResult.ShouldBeSuccessful();

        var itemId = createResult.Value!.MenuItemId;

        // Build baseline view after creation
        await DrainOutboxAsync();

        DateTimeOffset baselineRebuiltAt;
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            baselineRebuiltAt = view!.LastRebuiltAt;

            // Baseline: verify original price in view
            FullMenuViewAssertions.GetItemPriceAmount(view, itemId).Should().Be(10m, "baseline item should have original price");
        }

        // Update only price via UpdateMenuItemDetails (per current command shape)
        var update = new UpdateMenuItemDetailsCommand(
            RestaurantId: restaurantId,
            MenuItemId: itemId,
            Name: "Same Name",
            Description: "Same Description",
            Price: 12.34m,
            Currency: "USD",
            ImageUrl: null);

        // Act
        var updateResult = await SendAsync(update);
        updateResult.ShouldBeSuccessful();

        // First outbox drain processes MenuItemPriceChanged and rebuilds the view
        await DrainOutboxAsync();

        // Assert handler side-effects and post-condition after first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.MenuItems.EventHandlers.MenuItemPriceChangedEventHandler).FullName!;

            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1, "inbox must ensure idempotency");

            var processedOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains("MenuItemPriceChanged"))
                .ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.MenuJson.Should().NotBeNullOrWhiteSpace();

            // Rebuild time should have advanced
            view.LastRebuiltAt.Should().BeAfter(baselineRebuiltAt, "view should be rebuilt after price change event");

            // Post-condition: verify price updated in view
            FullMenuViewAssertions.GetItemPriceAmount(view, itemId).Should().Be(12.34m, "item price should be updated");
        }

        // Second drain to verify idempotent handler does nothing more
        DateTimeOffset lastRebuiltAtAfterFirstDrain;
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            lastRebuiltAtAfterFirstDrain = view!.LastRebuiltAt;
        }

        await DrainOutboxAsync();

        // Assert idempotency: second drain should not change anything
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.MenuItems.EventHandlers.MenuItemPriceChangedEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1, "draining again must not reprocess the same event");

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.LastRebuiltAt.Should().Be(lastRebuiltAtAfterFirstDrain, "idempotent second drain should not update LastRebuiltAt");

            // Verify price change persists
            FullMenuViewAssertions.GetItemPriceAmount(view, itemId).Should().Be(12.34m, "updated price should remain after idempotent drain");
        }
    }
}

