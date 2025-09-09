using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.MenuItems.Commands.CreateMenuItem;
using YummyZoom.Application.MenuItems.Commands.DeleteMenuItem;
using YummyZoom.Infrastructure.Persistence.EfCore;
using YummyZoom.Infrastructure.Persistence.EfCore.Models;
using YummyZoom.Infrastructure.Persistence.ReadModels.FullMenu;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Events;

using static Testing;

public class MenuItemDeletedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task DeleteMenuItem_ShouldRebuildFullMenuView_AndBeIdempotent()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = TestData.DefaultRestaurantId;
        var categoryId = TestData.GetMenuCategoryId("Main Dishes");

        var create = new CreateMenuItemCommand(
            RestaurantId: restaurantId,
            MenuCategoryId: categoryId,
            Name: $"Del-{Guid.NewGuid():N}",
            Description: "To delete",
            Price: 4m,
            Currency: "USD",
            ImageUrl: null,
            IsAvailable: true,
            DietaryTagIds: null);
        var createResult = await SendAsync(create);
        createResult.ShouldBeSuccessful();
        var itemId = createResult.Value!.MenuItemId;

        // Build baseline view (pre-condition)
        await DrainOutboxAsync();

        DateTimeOffset preDeleteRebuiltAt;
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            preDeleteRebuiltAt = view!.LastRebuiltAt;

            // Pre-condition: verify item exists in view before delete
            view.ShouldHaveItem(itemId, categoryId, "created item should be present before delete");
            view.ShouldHaveItemWithValues(itemId,
                expectedName: create.Name,
                expectedDescription: create.Description,
                expectedPriceAmount: create.Price,
                expectedCurrency: create.Currency,
                expectedAvailability: create.IsAvailable,
                because: "created item should have expected values before delete");
        }

        // Act: delete item
        var delete = new DeleteMenuItemCommand(RestaurantId: restaurantId, MenuItemId: itemId);
        var deleteResult = await SendAsync(delete);
        deleteResult.ShouldBeSuccessful();

        // First outbox drain processes MenuItemDeleted and rebuilds the view
        await DrainOutboxAsync();

        // Assert handler side-effects and post-condition after first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.MenuItems.EventHandlers.MenuItemDeletedEventHandler).FullName!;

            var inboxEntries = await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync();
            inboxEntries.Should().HaveCount(1, "inbox must ensure idempotency");

            var processedOutbox = await db.Set<OutboxMessage>().Where(m => m.Type.Contains("MenuItemDeleted")).ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.MenuJson.Should().NotBeNullOrWhiteSpace();

            // Rebuild time should have advanced
            view.LastRebuiltAt.Should().BeAfter(preDeleteRebuiltAt, "view should be rebuilt after delete event");

            // Post-condition: verify item is removed from view
            view.ShouldNotHaveItem(itemId, categoryId, "deleted item should not be present after delete");
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
            var handlerName = typeof(YummyZoom.Application.MenuItems.EventHandlers.MenuItemDeletedEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync();
            inboxEntries.Should().HaveCount(1, "draining again must not reprocess the same event");

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.LastRebuiltAt.Should().Be(lastRebuiltAtAfterFirstDrain, "idempotent second drain should not update LastRebuiltAt");

            // Verify item is still absent
            view.ShouldNotHaveItem(itemId, categoryId, "deleted item should remain absent after idempotent drain");
        }
    }
}
