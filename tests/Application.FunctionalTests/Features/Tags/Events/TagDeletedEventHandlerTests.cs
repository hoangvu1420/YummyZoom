using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Domain.TagEntity;
using YummyZoom.Domain.TagEntity.Enums;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Models;
using YummyZoom.Infrastructure.Data.ReadModels.FullMenu;

namespace YummyZoom.Application.FunctionalTests.Features.Tags.Events;

using static Testing;

/// <summary>
/// Ensures TagDeleted removes the tag from the legend but does not alter
/// existing item dietaryTagIds (by design, items store tag IDs independently).
/// Confirms idempotency (inbox count remains 1; subsequent drains do not change the view).
/// </summary>
public class TagDeletedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task TagDeleted_ShouldRebuildFullMenuView_RemoveLegendEntry_ButKeepItemIds_AndBeIdempotent()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = TestData.DefaultRestaurantId;

        var tagResult = Tag.Create("Contains Nuts", TagCategory.Allergen);
        tagResult.IsSuccess.Should().BeTrue();
        var tag = tagResult.Value;
        tag.ClearDomainEvents();
        await AddAsync(tag);

        var itemId = TestData.GetMenuItemId(Testing.TestData.MenuItems.ChocolateCake);
        // Critical: assignment ensures the tag appears in the legend before delete.
        await MenuTestDataFactory.AttachTagsToItemAsync(itemId, new List<Guid> { tag.Id.Value });

        await SendAsync(new YummyZoom.Application.Admin.Commands.RebuildFullMenu.RebuildFullMenuCommand(restaurantId));

        DateTimeOffset baselineRebuiltAt;
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var view = await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId);
            baselineRebuiltAt = view.LastRebuiltAt;

            var root = FullMenuViewAssertions.GetRoot(view);
            var legend = root.GetProperty("tagLegend").GetProperty("byId");
            legend.TryGetProperty(tag.Id.Value.ToString(), out var tagJson).Should().BeTrue();
            tagJson.GetProperty("name").GetString().Should().Be("Contains Nuts");

            // Item has the tag id
            var itemTagIds = FullMenuViewAssertions.GetItemDietaryTagIds(view, itemId);
            itemTagIds.Should().Contain(tag.Id.Value);
        }

        // Act: delete tag -> emits TagDeleted; legend entry should be removed, item ids remain.
        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var t = await db.Tags.FirstAsync(x => x.Id == tag.Id);
            var del = t.MarkAsDeleted(DateTimeOffset.UtcNow, "tester");
            del.IsSuccess.Should().BeTrue();
            await db.SaveChangesAsync();
        });

        await DrainOutboxAsync();

        // Assert first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.Tags.EventHandlers.TagDeletedEventHandler).FullName!;
            (await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync()).Should().HaveCount(1);

            var processedOutbox = await db.Set<OutboxMessage>().Where(m => m.Type.Contains("TagDeleted")).ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

            var view = await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId);
            view.LastRebuiltAt.Should().BeAfter(baselineRebuiltAt);

            var root = FullMenuViewAssertions.GetRoot(view);
            var legend = root.GetProperty("tagLegend").GetProperty("byId");
            legend.TryGetProperty(tag.Id.Value.ToString(), out var _).Should().BeFalse("deleted tag should be removed from legend");

            // Item still lists the tag id in its dietaryTagIds (by design)
            var itemTagIds = FullMenuViewAssertions.GetItemDietaryTagIds(view, itemId);
            itemTagIds.Should().Contain(tag.Id.Value);
        }

        // Idempotency
        DateTimeOffset afterFirst;
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            afterFirst = (await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId)).LastRebuiltAt;
        }
        await DrainOutboxAsync();
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.Tags.EventHandlers.TagDeletedEventHandler).FullName!;
            (await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync()).Should().HaveCount(1);
            (await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId)).LastRebuiltAt.Should().Be(afterFirst);
        }
    }
}
