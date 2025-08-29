using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.MenuItems.Commands.CreateMenuItem;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Models;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Events;

using static Testing;

/// <summary>
/// Functional tests for <see cref="MenuItemCreatedEventHandler"/> verifying:
/// 1. Outbox -> handler execution rebuilds FullMenuView via IMenuReadModelRebuilder.
/// 2. Inbox idempotency prevents duplicate side-effects on repeated outbox draining.
/// </summary>
public class MenuItemCreatedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task CreateMenuItem_ShouldRebuildFullMenuView_AndBeIdempotent()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = TestData.DefaultRestaurantId; // use default restaurant
        var menuCategoryId = TestData.GetMenuCategoryId("Main Dishes");

        var cmd = new CreateMenuItemCommand(
            RestaurantId: restaurantId,
            MenuCategoryId: menuCategoryId,
            Name: $"Created-{Guid.NewGuid():N}",
            Description: "A new tasty item",
            Price: 9.99m,
            Currency: "USD",
            ImageUrl: null,
            IsAvailable: true,
            DietaryTagIds: null);

        // Pre-condition: establish baseline view state (item should not exist yet)
        DateTimeOffset baselineRebuiltAt;
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);

            if (view != null)
            {
                baselineRebuiltAt = view.LastRebuiltAt;
                // Pre-condition: item should not exist yet (we don't have the itemId yet, so skip this check)
            }
            else
            {
                baselineRebuiltAt = DateTimeOffset.MinValue;
            }
        }

        // Act: send command which emits MenuItemCreated
        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();
        var itemId = result.Value!.MenuItemId;

        // First outbox drain processes MenuItemCreated and rebuilds the view
        await DrainOutboxAsync();

        // Assert handler side-effects and post-condition after first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.MenuItems.EventHandlers.MenuItemCreatedEventHandler).FullName!;

            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1, "inbox must ensure idempotency");

            var processedOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains("MenuItemCreated"))
                .ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.MenuJson.Should().NotBeNullOrWhiteSpace();

            // Rebuild time should have advanced
            view.LastRebuiltAt.Should().BeAfter(baselineRebuiltAt, "view should be rebuilt after create event");

            // Post-condition: verify item exists in view with correct values
            view.ShouldHaveItem(itemId, menuCategoryId, "created item should be present after create");
            view.ShouldHaveItemWithValues(itemId,
                expectedName: cmd.Name,
                expectedDescription: cmd.Description,
                expectedPriceAmount: cmd.Price,
                expectedCurrency: cmd.Currency,
                expectedAvailability: cmd.IsAvailable,
                because: "created item should have expected values");
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
            var handlerName = typeof(YummyZoom.Application.MenuItems.EventHandlers.MenuItemCreatedEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1, "draining again must not reprocess the same event");

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.LastRebuiltAt.Should().Be(lastRebuiltAtAfterFirstDrain, "idempotent second drain should not update LastRebuiltAt");

            // Verify item is still present with correct values
            view.ShouldHaveItem(itemId, menuCategoryId, "created item should remain present after idempotent drain");
        }
    }
}

