using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.MenuCategories.Commands.RemoveMenuCategory;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Models;
using YummyZoom.Infrastructure.Data.ReadModels.FullMenu;

namespace YummyZoom.Application.FunctionalTests.Features.MenuCategories.Events;

using static Testing;

/// <summary>
/// Functional tests for <see cref="MenuCategoryRemovedEventHandler"/> verifying:
/// 1. Outbox -> handler execution rebuilds FullMenuView via IFullMenuViewMaintainer.
/// 2. Inbox idempotency prevents duplicate side-effects on repeated outbox draining.
/// </summary>
public class MenuCategoryRemovedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task RemoveMenuCategory_ShouldRebuildFullMenuView_AndBeIdempotent()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = TestData.DefaultRestaurantId;

        // Create a new category to delete (so we don't affect other tests)
        var scenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = TestData.DefaultRestaurantId,
            EnabledMenu = true,
            CategoryCount = 1,
            CategoryGenerator = i => ($"Category to Delete", 1)
        });
        var categoryToDeleteId = scenario.CategoryIds.First();

        var cmd = new RemoveMenuCategoryCommand(
            RestaurantId: restaurantId,
            MenuCategoryId: categoryToDeleteId);

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

        // Act: send command which emits MenuCategoryRemoved
        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();

        // First outbox drain processes MenuCategoryRemoved and rebuilds the view
        await DrainOutboxAsync();

        // Assert handler side-effects and post-condition after first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.MenuCategories.EventHandlers.MenuCategoryRemovedEventHandler).FullName!;

            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1, "inbox must ensure idempotency");

            var processedOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains("MenuCategoryRemoved"))
                .ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.MenuJson.Should().NotBeNullOrWhiteSpace();

            // Rebuild time should have advanced
            view.LastRebuiltAt.Should().BeAfter(baselineRebuiltAt, "view should be rebuilt after menu category removed event");
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
            var handlerName = typeof(YummyZoom.Application.MenuCategories.EventHandlers.MenuCategoryRemovedEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1, "draining again must not reprocess the same event");

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.LastRebuiltAt.Should().Be(lastRebuiltAtAfterFirstDrain, "idempotent second drain should not update LastRebuiltAt");
        }
    }
}
