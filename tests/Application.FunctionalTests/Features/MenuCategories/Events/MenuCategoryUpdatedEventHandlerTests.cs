using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.MenuCategories.Commands.UpdateMenuCategoryDetails;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Models;
using YummyZoom.Infrastructure.Data.ReadModels.FullMenu;

namespace YummyZoom.Application.FunctionalTests.Features.MenuCategories.Events;

using static Testing;

/// <summary>
/// Functional tests for MenuCategory update event handlers verifying:
/// 1. Outbox -> handler execution rebuilds FullMenuView via IMenuReadModelRebuilder.
/// 2. Inbox idempotency prevents duplicate side-effects on repeated outbox draining.
/// </summary>
public class MenuCategoryUpdatedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task UpdateMenuCategoryDetails_ShouldRebuildFullMenuView_AndBeIdempotent()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = TestData.DefaultRestaurantId;
        var categoryId = TestData.GetMenuCategoryId("Main Dishes");

        var cmd = new UpdateMenuCategoryDetailsCommand(
            RestaurantId: restaurantId,
            MenuCategoryId: categoryId,
            Name: $"Updated Category {Guid.NewGuid():N}",
            DisplayOrder: 5);

        // Pre-condition: establish baseline view state
        DateTimeOffset baselineRebuiltAt;
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);

            if (view != null)
            {
                baselineRebuiltAt = view.LastRebuiltAt;
            }
            else
            {
                baselineRebuiltAt = DateTimeOffset.MinValue;
            }
        }

        // Act: send command which emits MenuCategoryNameUpdated and MenuCategoryDisplayOrderUpdated
        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();

        // First outbox drain processes the events and rebuilds the view
        await DrainOutboxAsync();

        // Assert handler side-effects and post-condition after first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Check for both possible event handlers
            var nameHandlerName = typeof(YummyZoom.Application.MenuCategories.EventHandlers.MenuCategoryNameUpdatedEventHandler).FullName!;
            var displayOrderHandlerName = typeof(YummyZoom.Application.MenuCategories.EventHandlers.MenuCategoryDisplayOrderUpdatedEventHandler).FullName!;

            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == nameHandlerName || x.Handler == displayOrderHandlerName)
                .ToListAsync();
            inboxEntries.Should().NotBeEmpty("inbox must ensure idempotency for category update events");

            var processedOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains("MenuCategoryNameUpdated") || m.Type.Contains("MenuCategoryDisplayOrderUpdated"))
                .ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.MenuJson.Should().NotBeNullOrWhiteSpace();

            // Rebuild time should have advanced
            view.LastRebuiltAt.Should().BeAfter(baselineRebuiltAt, "view should be rebuilt after menu category update events");
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
            var nameHandlerName = typeof(YummyZoom.Application.MenuCategories.EventHandlers.MenuCategoryNameUpdatedEventHandler).FullName!;
            var displayOrderHandlerName = typeof(YummyZoom.Application.MenuCategories.EventHandlers.MenuCategoryDisplayOrderUpdatedEventHandler).FullName!;
            
            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == nameHandlerName || x.Handler == displayOrderHandlerName)
                .ToListAsync();
            inboxEntries.Should().NotBeEmpty("draining again must not reprocess the same events");

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.LastRebuiltAt.Should().Be(lastRebuiltAtAfterFirstDrain, "idempotent second drain should not update LastRebuiltAt");
        }
    }
}
