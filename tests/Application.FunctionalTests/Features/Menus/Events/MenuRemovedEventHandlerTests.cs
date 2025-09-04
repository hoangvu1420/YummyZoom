using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Models;

namespace YummyZoom.Application.FunctionalTests.Features.Menus.Events;

using static Testing;

public class MenuRemovedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task RemovingMenu_ShouldDeleteFullMenuView_AndBeIdempotent()
    {
        // Arrange: set up user and create an enabled menu scenario
        await RunAsRestaurantStaffAsync("staff@restaurant.com", TestData.DefaultRestaurantId);

        var scenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = TestData.DefaultRestaurantId,
            EnabledMenu = true,
            CategoryCount = 0 // categories/items not required for this test
        });

        var restaurantId = scenario.RestaurantId;
        var menuId = scenario.MenuId;

        // Act: soft delete the menu to emit MenuRemoved
        var id = MenuId.Create(menuId);
        var menu = await FindAsync<Menu>(id);
        menu!.MarkAsDeleted(DateTimeOffset.UtcNow);
        await UpdateAsync(menu);

        // First outbox drain processes MenuRemoved and deletes the view
        await DrainOutboxAsync();

        // Assert after first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.Menus.EventHandlers.MenuRemovedEventHandler).FullName!;

            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1, "inbox must ensure idempotency");

            var processedOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains("MenuRemoved"))
                .ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().BeNull("because the FullMenuView should be deleted when the menu is removed");
        }

        // Second drain to verify idempotent handler does nothing more
        await DrainOutboxAsync();

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.Menus.EventHandlers.MenuRemovedEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1, "draining again must not reprocess the same event");

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().BeNull("because the FullMenuView should remain deleted after idempotent second drain");
        }
    }
}

