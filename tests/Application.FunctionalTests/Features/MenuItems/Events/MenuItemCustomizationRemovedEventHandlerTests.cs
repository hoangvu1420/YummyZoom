using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.MenuItems.Commands.CreateMenuItem;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Models;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data.ReadModels.FullMenu;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Events;

using static Testing;

public class MenuItemCustomizationRemovedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task RemoveCustomization_ShouldRebuildFullMenuView_AndBeIdempotent()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = TestData.DefaultRestaurantId;
        var categoryId = TestData.GetMenuCategoryId("Main Dishes");

        // Create a new item
        var create = new CreateMenuItemCommand(
            RestaurantId: restaurantId,
            MenuCategoryId: categoryId,
            Name: $"Cust-{Guid.NewGuid():N}",
            Description: "Before customization",
            Price: 10m,
            Currency: "USD",
            ImageUrl: null,
            IsAvailable: true,
            DietaryTagIds: null);
        var createResult = await SendAsync(create);
        createResult.ShouldBeSuccessful();
        var itemId = createResult.Value!.MenuItemId;

        // Create a customization group for this restaurant directly in domain
        var groupResult = CustomizationGroup.Create(RestaurantId.Create(restaurantId), "Add-ons", 0, 1);
        groupResult.ShouldBeSuccessful();
        var group = groupResult.Value;
        group.AddChoice("Extra Sauce", new Money(0.5m, "USD"), false, 1);
        await AddAsync(group);

        // Assign first
        var item = await FindAsync<MenuItem>(MenuItemId.Create(itemId));
        var applied = AppliedCustomization.Create(group.Id, "Add-ons", 1);
        var assignResult = item!.AssignCustomizationGroup(applied);
        assignResult.ShouldBeSuccessful();
        await UpdateAsync(item);

        // Build baseline view after assignment
        await DrainOutboxAsync();

        DateTimeOffset baselineRebuiltAt;
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            baselineRebuiltAt = view!.LastRebuiltAt;

            // Baseline: verify customization group is assigned in view
            var baselineGroups = FullMenuViewAssertions.GetItemCustomizationGroupIds(view, itemId);
            baselineGroups.Should().Contain(group.Id.Value, "baseline item should have assigned customization group");
        }

        // Remove it
        var removeResult = item.RemoveCustomizationGroup(group.Id);
        removeResult.ShouldBeSuccessful();
        await UpdateAsync(item);

        // Act: first outbox drain processes MenuItemCustomizationRemoved and rebuilds the view
        await DrainOutboxAsync();

        // Assert handler side-effects and post-condition after first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.MenuItems.EventHandlers.MenuItemCustomizationRemovedEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync();
            inboxEntries.Should().HaveCount(1, "inbox must ensure idempotency");

            var processedOutbox = await db.Set<OutboxMessage>().Where(m => m.Type.Contains("MenuItemCustomizationRemoved")).ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.MenuJson.Should().NotBeNullOrWhiteSpace();

            // Rebuild time should have advanced
            view.LastRebuiltAt.Should().BeAfter(baselineRebuiltAt, "view should be rebuilt after customization removal event");

            // Post-condition: verify customization group removed from view
            var remainingGroups = FullMenuViewAssertions.GetItemCustomizationGroupIds(view, itemId);
            remainingGroups.Should().NotContain(group.Id.Value, "customization group should be removed from item");
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
            var handlerName = typeof(YummyZoom.Application.MenuItems.EventHandlers.MenuItemCustomizationRemovedEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync();
            inboxEntries.Should().HaveCount(1, "draining again must not reprocess the same event");

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.LastRebuiltAt.Should().Be(lastRebuiltAtAfterFirstDrain, "idempotent second drain should not update LastRebuiltAt");

            // Verify customization removal persists
            var persistedGroups = FullMenuViewAssertions.GetItemCustomizationGroupIds(view, itemId);
            persistedGroups.Should().NotContain(group.Id.Value, "customization group should remain removed after idempotent drain");
        }
    }
}

