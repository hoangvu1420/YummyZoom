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

        // Act: send command which emits MenuItemCreated, then drain outbox twice to validate idempotency
        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();

        await DrainOutboxAsync();
        await DrainOutboxAsync(); // idempotency

        // Assert: Inbox/Outbox status first (to diagnose handler execution)
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.MenuItems.EventHandlers.MenuItemCreatedEventHandler).FullName!;

            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1);

            var processedOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains("MenuItemCreated"))
                .ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
        }

        // Assert: FullMenuView row exists and is updated
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.MenuJson.Should().NotBeNullOrWhiteSpace();
            view.LastRebuiltAt.Should().BeAfter(DateTimeOffset.MinValue);
        }
    }
}

