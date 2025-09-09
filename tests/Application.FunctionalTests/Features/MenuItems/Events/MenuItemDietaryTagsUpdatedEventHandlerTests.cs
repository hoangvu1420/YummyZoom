using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.MenuItems.Commands.CreateMenuItem;
using YummyZoom.Application.MenuItems.Commands.UpdateMenuItemDietaryTags;
using YummyZoom.Infrastructure.Persistence.EfCore;
using YummyZoom.Infrastructure.Persistence.EfCore.Models;
using YummyZoom.Infrastructure.Persistence.ReadModels.FullMenu;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Events;

using static Testing;

public class MenuItemDietaryTagsUpdatedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task UpdateDietaryTags_ShouldRebuildFullMenuView_AndBeIdempotent()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = TestData.DefaultRestaurantId;
        var categoryId = TestData.GetMenuCategoryId("Main Dishes");

        var create = new CreateMenuItemCommand(
            RestaurantId: restaurantId,
            MenuCategoryId: categoryId,
            Name: $"Tags-{Guid.NewGuid():N}",
            Description: "Before tags",
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

            // Baseline: verify no dietary tags in view
            var baselineTags = FullMenuViewAssertions.GetItemDietaryTagIds(view, itemId);
            baselineTags.Should().BeEmpty("baseline item should have no dietary tags");
        }

        // Update tags: use two generated tag IDs
        var tags = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var update = new UpdateMenuItemDietaryTagsCommand(
            RestaurantId: restaurantId,
            MenuItemId: itemId,
            DietaryTagIds: tags);

        // Act
        var updateResult = await SendAsync(update);
        updateResult.ShouldBeSuccessful();

        // First outbox drain processes MenuItemDietaryTagsUpdated and rebuilds the view
        await DrainOutboxAsync();

        // Assert handler side-effects and post-condition after first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.MenuItems.EventHandlers.MenuItemDietaryTagsUpdatedEventHandler).FullName!;

            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1, "inbox must ensure idempotency");

            var processedOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains("MenuItemDietaryTagsUpdated"))
                .ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.MenuJson.Should().NotBeNullOrWhiteSpace();

            // Rebuild time should have advanced
            view.LastRebuiltAt.Should().BeAfter(baselineRebuiltAt, "view should be rebuilt after dietary tags update event");

            // Post-condition: verify dietary tags updated in view
            var updatedTags = FullMenuViewAssertions.GetItemDietaryTagIds(view, itemId);
            updatedTags.Should().BeEquivalentTo(tags, "item dietary tags should be updated");
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
            var handlerName = typeof(YummyZoom.Application.MenuItems.EventHandlers.MenuItemDietaryTagsUpdatedEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1, "draining again must not reprocess the same event");

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.LastRebuiltAt.Should().Be(lastRebuiltAtAfterFirstDrain, "idempotent second drain should not update LastRebuiltAt");

            // Verify dietary tags persist
            var persistedTags = FullMenuViewAssertions.GetItemDietaryTagIds(view, itemId);
            persistedTags.Should().BeEquivalentTo(tags, "dietary tags should remain after idempotent drain");
        }
    }
}

