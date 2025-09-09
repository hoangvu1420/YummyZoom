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

public class CustomizationChoicesReorderedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task ReorderChoices_ShouldRebuildFullMenuView_AndBeIdempotent()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = RestaurantId.Create(TestData.DefaultRestaurantId);

        var groupResult = CustomizationGroup.Create(restaurantId, "Add-ons", 0, 3);
        groupResult.IsSuccess.Should().BeTrue();
        var group = groupResult.Value;
        group.AddChoice("A", new Money(0.50m, "USD"), false, 1);
        group.AddChoice("B", new Money(0.75m, "USD"), false, 2);
        group.AddChoice("C", new Money(1.00m, "USD"), false, 3);
        var choices = group.Choices.ToList();
        group.ClearDomainEvents();
        await AddAsync(group);

        // Ensure inclusion and capture baseline order
        var itemId = TestData.GetMenuItemId(Testing.TestData.MenuItems.ChocolateCake);
        var displayTitle = "Extras";
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
            // Pre-condition: initial order A,B,C
            view.Should().NotBeNull();
            var optionIds = FullMenuViewAssertions.GetGroupOptionIds(view!, group.Id.Value);
            optionIds.Should().Equal(choices.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).Select(c => c.Id.Value));
        }

        // Act: reorder -> raises CustomizationChoicesReordered
        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var g = await db.CustomizationGroups.Include(x => x.Choices).FirstAsync(x => x.Id == group.Id);
            var current = g.Choices.ToList();
            var reorder = g.ReorderChoices(new List<(ChoiceId choiceId, int newDisplayOrder)>
            {
                (current[0].Id, 3),
                (current[1].Id, 1),
                (current[2].Id, 2)
            });
            reorder.IsSuccess.Should().BeTrue();
            await db.SaveChangesAsync();
        });
        await DrainOutboxAsync();

        // Assert first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.CustomizationGroups.EventHandlers.CustomizationChoicesReorderedEventHandler).FullName!;
            (await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync()).Should().HaveCount(1);
            var processedOutbox = await db.Set<OutboxMessage>().Where(m => m.Type.Contains("CustomizationChoicesReordered")).ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
            var view = await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId.Value);
            view.LastRebuiltAt.Should().BeAfter(baselineRebuiltAt);

            // Post-condition: order B(1), C(2), A(3)
            var current = await db.CustomizationGroups.Include(x => x.Choices).FirstAsync(x => x.Id == group.Id);
            var A = current.Choices.First(c => c.Name == "A");
            var B = current.Choices.First(c => c.Name == "B");
            var C = current.Choices.First(c => c.Name == "C");
            view.ShouldHaveGroupOptionsInOrder(group.Id.Value, B.Id.Value, C.Id.Value, A.Id.Value);
            view.ShouldHaveGroupOption(group.Id.Value, B.Id.Value, B.Name, B.PriceAdjustment.Amount, B.PriceAdjustment.Currency, B.IsDefault, B.DisplayOrder);
            view.ShouldHaveGroupOption(group.Id.Value, C.Id.Value, C.Name, C.PriceAdjustment.Amount, C.PriceAdjustment.Currency, C.IsDefault, C.DisplayOrder);
            view.ShouldHaveGroupOption(group.Id.Value, A.Id.Value, A.Name, A.PriceAdjustment.Amount, A.PriceAdjustment.Currency, A.IsDefault, A.DisplayOrder);
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
            var handlerName = typeof(YummyZoom.Application.CustomizationGroups.EventHandlers.CustomizationChoicesReorderedEventHandler).FullName!;
            (await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync()).Should().HaveCount(1);
            (await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId.Value)).LastRebuiltAt.Should().Be(afterFirst);
        }
    }
}
