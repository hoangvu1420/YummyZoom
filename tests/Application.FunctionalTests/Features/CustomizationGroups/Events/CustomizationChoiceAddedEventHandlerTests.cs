using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Application.Admin.Commands.RebuildFullMenu;
using YummyZoom.Infrastructure.Persistence.EfCore;
using YummyZoom.Infrastructure.Persistence.EfCore.Models;
using YummyZoom.Infrastructure.Persistence.ReadModels.FullMenu;

namespace YummyZoom.Application.FunctionalTests.Features.CustomizationGroups.Events;

using static Testing;

public class CustomizationChoiceAddedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task AddChoice_ShouldRebuildFullMenuView_AndBeIdempotent()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = RestaurantId.Create(TestData.DefaultRestaurantId);

        var groupResult = CustomizationGroup.Create(restaurantId, "Add-ons", 0, 2);
        groupResult.IsSuccess.Should().BeTrue();
        var group = groupResult.Value;
        group.ClearDomainEvents();
        await AddAsync(group);

        // Ensure group inclusion by assigning to an item, then rebuild baseline view
        var itemId = TestData.GetMenuItemId(Testing.TestData.MenuItems.CaesarSalad);
        var displayTitle = "Pick add-ons";
        var displayOrder = 1;
        await MenuTestDataFactory.AttachCustomizationsToItemAsync(itemId, new List<(Guid GroupId, string Title, int Order)>
        {
            (group.Id.Value, displayTitle, displayOrder)
        });

        var rebuild = await SendAsync(new RebuildFullMenuCommand(restaurantId.Value));
        rebuild.IsSuccess.Should().BeTrue();

        DateTimeOffset baselineRebuiltAt;
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var view = await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId.Value);
            baselineRebuiltAt = view.LastRebuiltAt;
            // Pre-condition: group exists with no options
            view.ShouldHaveCustomizationGroup(group.Id.Value, group.GroupName, group.MinSelections, group.MaxSelections);
            FullMenuViewAssertions.GetGroupOptionIds(view, group.Id.Value).Should().BeEmpty();
        }

        // Act: Add choice (raises CustomizationChoiceAdded)
        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var g = await db.CustomizationGroups.Include(x => x.Choices).FirstAsync(x => x.Id == group.Id);
            var add = g.AddChoice("Extra Cheese", new Money(1.00m, "USD"), isDefault: false, displayOrder: 1);
            add.IsSuccess.Should().BeTrue();
            await db.SaveChangesAsync();
        });
        await DrainOutboxAsync();

        // Assert first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.CustomizationGroups.EventHandlers.CustomizationChoiceAddedEventHandler).FullName!;

            (await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync()).Should().HaveCount(1);
            var processedOutbox = await db.Set<OutboxMessage>().Where(m => m.Type.Contains("CustomizationChoiceAdded")).ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
            var view = await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId.Value);
            view.LastRebuiltAt.Should().BeAfter(baselineRebuiltAt);

            // Post-condition: group has the new option with expected fields and order
            var choice = (await db.CustomizationGroups.Include(x => x.Choices).FirstAsync(x => x.Id == group.Id)).Choices.First();
            view.ShouldHaveGroupOption(group.Id.Value, choice.Id.Value, "Extra Cheese", 1.00m, "USD", false, 1);
            view.ShouldHaveGroupOptionsInOrder(group.Id.Value, choice.Id.Value);
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
            var handlerName = typeof(YummyZoom.Application.CustomizationGroups.EventHandlers.CustomizationChoiceAddedEventHandler).FullName!;
            (await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync()).Should().HaveCount(1);
            (await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId.Value)).LastRebuiltAt.Should().Be(afterFirst);
        }
    }
}
