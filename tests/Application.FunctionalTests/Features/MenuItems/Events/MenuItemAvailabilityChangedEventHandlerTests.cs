using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.MenuItems.Commands.ChangeMenuItemAvailability;
using YummyZoom.Application.MenuItems.Commands.CreateMenuItem;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Models;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Events;

using static Testing;

public class MenuItemAvailabilityChangedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task ChangeAvailability_ShouldRebuildFullMenuView_AndBeIdempotent()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = TestData.DefaultRestaurantId;
        var categoryId = TestData.GetMenuCategoryId("Main Dishes");

        // Create a menu item first
        var create = new CreateMenuItemCommand(
            RestaurantId: restaurantId,
            MenuCategoryId: categoryId,
            Name: $"Avail-{Guid.NewGuid():N}",
            Description: "Item for availability test",
            Price: 5m,
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

            // Baseline: verify original availability in view
            FullMenuViewAssertions.GetItemAvailability(view, itemId).Should().BeTrue("baseline item should be available");
        }

        // Toggle availability to false
        var toggle = new ChangeMenuItemAvailabilityCommand(
            RestaurantId: restaurantId,
            MenuItemId: itemId,
            IsAvailable: false);

        // Act
        var toggleResult = await SendAsync(toggle);
        toggleResult.ShouldBeSuccessful();

        // First outbox drain processes MenuItemAvailabilityChanged and rebuilds the view
        await DrainOutboxAsync();

        // Assert handler side-effects and post-condition after first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.MenuItems.EventHandlers.MenuItemAvailabilityChangedEventHandler).FullName!;

            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1, "inbox must ensure idempotency");

            var processedOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains("MenuItemAvailabilityChanged"))
                .ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.MenuJson.Should().NotBeNullOrWhiteSpace();

            // Rebuild time should have advanced
            view.LastRebuiltAt.Should().BeAfter(baselineRebuiltAt, "view should be rebuilt after availability change event");

            // Post-condition: verify availability changed in view
            FullMenuViewAssertions.GetItemAvailability(view, itemId).Should().BeFalse("item availability should be updated to false");
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
            var handlerName = typeof(YummyZoom.Application.MenuItems.EventHandlers.MenuItemAvailabilityChangedEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1, "draining again must not reprocess the same event");

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.LastRebuiltAt.Should().Be(lastRebuiltAtAfterFirstDrain, "idempotent second drain should not update LastRebuiltAt");

            // Verify availability change persists
            FullMenuViewAssertions.GetItemAvailability(view, itemId).Should().BeFalse("item should remain unavailable after idempotent drain");
        }
    }
}

