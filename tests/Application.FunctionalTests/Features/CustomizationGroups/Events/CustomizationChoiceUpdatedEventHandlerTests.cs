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

public class CustomizationChoiceUpdatedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task UpdateChoice_ShouldRebuildFullMenuView_AndBeIdempotent()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = RestaurantId.Create(TestData.DefaultRestaurantId);

        var groupResult = CustomizationGroup.Create(restaurantId, "Add-ons", 0, 3);
        groupResult.IsSuccess.Should().BeTrue();
        var group = groupResult.Value;
        group.AddChoice("A", new Money(0.50m, "USD"), false, 1);
        var targetChoice = group.Choices.First(c => c.Name == "A");
        group.ClearDomainEvents();
        await AddAsync(group);

        // Ensure inclusion and capture baseline state
        var itemId = TestData.GetMenuItemId(Testing.TestData.MenuItems.BuffaloWings);
        var displayTitle = "Customize";
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
            // Pre-condition: verify original fields present
            view.Should().NotBeNull();
            view!.ShouldHaveGroupOption(group.Id.Value, targetChoice.Id.Value, "A", 0.50m, "USD", false, 1);
        }

        // Act: update choice -> raises CustomizationChoiceUpdated
        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var g = await db.CustomizationGroups.Include(x => x.Choices).FirstAsync(x => x.Id == group.Id);
            var ch = g.Choices.First(c => c.Id == targetChoice.Id);
            var upd = g.UpdateChoice(ch.Id, "A+", new Money(0.60m, "USD"), isDefault: true, displayOrder: 2);
            upd.IsSuccess.Should().BeTrue();
            await db.SaveChangesAsync();
        });
        await DrainOutboxAsync();

        // Assert first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.CustomizationGroups.EventHandlers.CustomizationChoiceUpdatedEventHandler).FullName!;
            (await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync()).Should().HaveCount(1);
            var processedOutbox = await db.Set<OutboxMessage>().Where(m => m.Type.Contains("CustomizationChoiceUpdated")).ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
            var view = await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId.Value);
            view.LastRebuiltAt.Should().BeAfter(baselineRebuiltAt);

            // Post-condition: updated fields reflected and order consistent
            var updated = (await db.CustomizationGroups.Include(x => x.Choices).FirstAsync(x => x.Id == group.Id)).Choices.First(c => c.Id == targetChoice.Id);
            view.ShouldHaveGroupOption(group.Id.Value, updated.Id.Value, "A+", 0.60m, "USD", true, 2);
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
            var handlerName = typeof(YummyZoom.Application.CustomizationGroups.EventHandlers.CustomizationChoiceUpdatedEventHandler).FullName!;
            (await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync()).Should().HaveCount(1);
            (await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId.Value)).LastRebuiltAt.Should().Be(afterFirst);
        }
    }
}
