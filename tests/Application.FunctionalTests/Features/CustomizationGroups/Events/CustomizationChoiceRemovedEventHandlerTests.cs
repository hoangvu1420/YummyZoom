using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Application.Admin.Commands.RebuildFullMenu;
using YummyZoom.Infrastructure.Persistence.EfCore;
using YummyZoom.Infrastructure.Persistence.EfCore.Models;
using YummyZoom.Infrastructure.Persistence.ReadModels.FullMenu;

namespace YummyZoom.Application.FunctionalTests.Features.CustomizationGroups.Events;

using static Testing;

public class CustomizationChoiceRemovedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task RemoveChoice_ShouldRebuildFullMenuView_AndBeIdempotent()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = RestaurantId.Create(TestData.DefaultRestaurantId);

        var groupResult = CustomizationGroup.Create(restaurantId, "Add-ons", 0, 3);
        groupResult.IsSuccess.Should().BeTrue();
        var group = groupResult.Value;
        group.AddChoice("A", new Money(0.50m, "USD"), false, 1);
        group.AddChoice("B", new Money(0.75m, "USD"), false, 2);
        var targetChoiceId = group.Choices.First(c => c.Name == "A").Id;

        // Persist without emitting previous events
        group.ClearDomainEvents();
        await AddAsync(group);

        // Ensure inclusion and capture baseline options
        var itemId = TestData.GetMenuItemId(Testing.TestData.MenuItems.GrilledSalmon);
        var displayTitle = "Choose extras";
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
            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId.Value);
            baselineRebuiltAt = view?.LastRebuiltAt ?? DateTimeOffset.MinValue;
            // Pre-condition: two options are present
            view.Should().NotBeNull();
            var optionIds = FullMenuViewAssertions.GetGroupOptionIds(view!, group.Id.Value);
            optionIds.Should().HaveCount(2);
            optionIds.Should().Contain(targetChoiceId.Value);
        }

        // Act: remove choice -> raises CustomizationChoiceRemoved
        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var g = await db.CustomizationGroups.Include(x => x.Choices).FirstAsync(x => x.Id == group.Id);
            var rem = g.RemoveChoice(targetChoiceId);
            rem.IsSuccess.Should().BeTrue();
            await db.SaveChangesAsync();
        });
        await DrainOutboxAsync();

        // Assert first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.CustomizationGroups.EventHandlers.CustomizationChoiceRemovedEventHandler).FullName!;
            (await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync()).Should().HaveCount(1);
            var processedOutbox = await db.Set<OutboxMessage>().Where(m => m.Type.Contains("CustomizationChoiceRemoved")).ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
            var view = await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId.Value);
            view.LastRebuiltAt.Should().BeAfter(baselineRebuiltAt);

            // Post-condition: removed choice not present, remaining options unchanged (order except removed)
            view.ShouldNotHaveGroupOption(group.Id.Value, targetChoiceId.Value);
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
            var handlerName = typeof(YummyZoom.Application.CustomizationGroups.EventHandlers.CustomizationChoiceRemovedEventHandler).FullName!;
            (await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync()).Should().HaveCount(1);
            (await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId.Value)).LastRebuiltAt.Should().Be(afterFirst);
        }
    }
}
