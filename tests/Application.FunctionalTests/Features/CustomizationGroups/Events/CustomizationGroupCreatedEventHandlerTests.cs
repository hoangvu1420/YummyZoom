using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Models;
using YummyZoom.Application.Admin.Commands.RebuildFullMenu;
using YummyZoom.Infrastructure.Data.ReadModels.FullMenu;

namespace YummyZoom.Application.FunctionalTests.Features.CustomizationGroups.Events;

using static Testing;

/// <summary>
/// Functional tests for CustomizationGroupCreatedEventHandler verifying:
/// 1. Outbox -> handler execution rebuilds FullMenuView via IMenuReadModelRebuilder.
/// 2. Inbox idempotency prevents duplicate side-effects on repeated outbox draining.
/// </summary>
public class CustomizationGroupCreatedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task CreateCustomizationGroup_ShouldRebuildFullMenuView_AndBeIdempotent()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = RestaurantId.Create(TestData.DefaultRestaurantId);

        // Pre-condition: establish baseline view state
        DateTimeOffset baselineRebuiltAt;
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId.Value);
            baselineRebuiltAt = view?.LastRebuiltAt ?? DateTimeOffset.MinValue;
        }

        // Act: create a new customization group aggregate which emits CustomizationGroupCreated
        var groupResult = CustomizationGroup.Create(
            restaurantId,
            groupName: "Add-ons " + Guid.NewGuid().ToString("N"),
            minSelections: 0,
            maxSelections: 2);
        groupResult.IsSuccess.Should().BeTrue();
        var group = groupResult.Value;

        // Persist (keep domain events for outbox)
        await AddAsync(group);

        // Ensure the group will appear in the FullMenuView by assigning it to an existing item
        var itemId = TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        var displayTitle = "Choose add-ons";
        var displayOrder = 1;
        await MenuTestDataFactory.AttachCustomizationsToItemAsync(itemId, new List<(Guid GroupId, string Title, int Order)>
        {
            (group.Id.Value, displayTitle, displayOrder)
        });

        // First outbox drain processes CustomizationGroupCreated and rebuilds the view
        await DrainOutboxAsync();

        // Assert handler side-effects and post-condition after first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.CustomizationGroups.EventHandlers.CustomizationGroupCreatedEventHandler).FullName!;

            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1, "inbox must ensure idempotency");

            var processedOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains("CustomizationGroupCreated"))
                .ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

            var view = await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId.Value);
            view.Should().NotBeNull();
            view.MenuJson.Should().NotBeNullOrWhiteSpace();

            // Rebuild time should have advanced
            view.LastRebuiltAt.Should().BeAfter(baselineRebuiltAt, "view should be rebuilt after group created event");

            // Post-condition: the group should exist in customizationGroups and have no options yet
            view.ShouldHaveCustomizationGroup(group.Id.Value, group.GroupName, group.MinSelections, group.MaxSelections);
            FullMenuViewAssertions.GetGroupOptionIds(view, group.Id.Value).Should().BeEmpty();

            // And the item should list the customization group assignment
            view.ShouldHaveItemCustomizationGroup(itemId, group.Id.Value, displayTitle, displayOrder);
        }

        // Second drain to verify idempotent handler does nothing more
        DateTimeOffset lastRebuiltAtAfterFirstDrain;
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId.Value);
            lastRebuiltAtAfterFirstDrain = view!.LastRebuiltAt;
        }

        await DrainOutboxAsync();

        // Assert idempotency: second drain should not change anything
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.CustomizationGroups.EventHandlers.CustomizationGroupCreatedEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1, "draining again must not reprocess the same event");

            var view = await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId.Value);
            view.Should().NotBeNull();
            view.LastRebuiltAt.Should().Be(lastRebuiltAtAfterFirstDrain, "idempotent second drain should not update LastRebuiltAt");

            // Group and assignment remain stable
            view.ShouldHaveCustomizationGroup(group.Id.Value, group.GroupName, group.MinSelections, group.MaxSelections);
            view.ShouldHaveItemCustomizationGroup(itemId, group.Id.Value, displayTitle, displayOrder);
        }
    }
}
