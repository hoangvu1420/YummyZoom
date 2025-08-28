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

        // Remove it
        var removeResult = item.RemoveCustomizationGroup(group.Id);
        removeResult.ShouldBeSuccessful();
        await UpdateAsync(item);

        // Act: drain outbox twice
        await DrainOutboxAsync();
        await DrainOutboxAsync();

        // Assert inbox/outbox
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.MenuItems.EventHandlers.MenuItemCustomizationRemovedEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync();
            inboxEntries.Should().HaveCount(1);

            var processedOutbox = await db.Set<OutboxMessage>().Where(m => m.Type.Contains("MenuItemCustomizationRemoved")).ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
        }

        // Assert view exists
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var view = await db.Set<FullMenuView>().FirstOrDefaultAsync(v => v.RestaurantId == restaurantId);
            view.Should().NotBeNull();
            view!.MenuJson.Should().NotBeNullOrWhiteSpace();
        }
    }
}

