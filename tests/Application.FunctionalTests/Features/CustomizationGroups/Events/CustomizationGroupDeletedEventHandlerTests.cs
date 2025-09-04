using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Models;
using YummyZoom.Application.Admin.Commands.RebuildFullMenu;

namespace YummyZoom.Application.FunctionalTests.Features.CustomizationGroups.Events;

using static Testing;

public class CustomizationGroupDeletedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task DeleteCustomizationGroup_ShouldRebuildFullMenuView_AndBeIdempotent()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = RestaurantId.Create(TestData.DefaultRestaurantId);

        // Seed: create group but do NOT emit events during seed
        var groupResult = CustomizationGroup.Create(restaurantId, "Temp Group", 0, 1);
        groupResult.IsSuccess.Should().BeTrue();
        var group = groupResult.Value;
        group.ClearDomainEvents();
        await AddAsync(group);

        // Assign the group to an existing item so it participates in the view
        var itemId = TestData.GetMenuItemId(Testing.TestData.MenuItems.MargheritaPizza);
        var displayTitle = "Pick one";
        var displayOrder = 1;
        await MenuTestDataFactory.AttachCustomizationsToItemAsync(itemId, new List<(Guid GroupId, string Title, int Order)>
        {
            (group.Id.Value, displayTitle, displayOrder)
        });

        // Pre-condition: rebuild to include the group in the view before deletion
        var rebuild = await SendAsync(new RebuildFullMenuCommand(restaurantId.Value));
        rebuild.IsSuccess.Should().BeTrue();

        // Baseline
        DateTimeOffset baselineRebuiltAt;
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var view = await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId.Value);
            baselineRebuiltAt = view.LastRebuiltAt;
            // Pre-condition: group is present and item assignment exists
            view.ShouldHaveCustomizationGroup(group.Id.Value, group.GroupName, group.MinSelections, group.MaxSelections);
            view.ShouldHaveItemCustomizationGroup(itemId, group.Id.Value, displayTitle, displayOrder);
        }

        // Act: delete group -> emits CustomizationGroupDeleted
        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var g = await db.CustomizationGroups.FirstAsync(x => x.Id == group.Id);
            var del = g.MarkAsDeleted(DateTimeOffset.UtcNow, "tester");
            del.IsSuccess.Should().BeTrue();
            await db.SaveChangesAsync();
        });

        await DrainOutboxAsync();

        // Assert first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.CustomizationGroups.EventHandlers.CustomizationGroupDeletedEventHandler).FullName!;

            var inboxEntries = await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync();
            inboxEntries.Should().HaveCount(1);

            var processedOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains("CustomizationGroupDeleted"))
                .ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId.Value);
            view.Should().NotBeNull();
            view!.LastRebuiltAt.Should().BeAfter(baselineRebuiltAt);

            // Post-condition: group details removed, but item assignment remains
            view.ShouldNotHaveCustomizationGroup(group.Id.Value);
            view.ShouldHaveItemCustomizationGroup(itemId, group.Id.Value, displayTitle, displayOrder);
        }

        // Idempotency
        DateTimeOffset afterFirst;
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            afterFirst = (await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId.Value)).LastRebuiltAt;
        }
        await DrainOutboxAsync();
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.CustomizationGroups.EventHandlers.CustomizationGroupDeletedEventHandler).FullName!;
            var inbox = await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync();
            inbox.Should().HaveCount(1);
            (await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId.Value)).LastRebuiltAt.Should().Be(afterFirst);
        }
    }
}
