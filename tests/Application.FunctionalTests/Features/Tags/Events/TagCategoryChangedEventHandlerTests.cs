using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Domain.TagEntity;
using YummyZoom.Domain.TagEntity.Enums;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Models;

namespace YummyZoom.Application.FunctionalTests.Features.Tags.Events;

using static Testing;

/// <summary>
/// Verifies TagCategoryChanged triggers rebuild for restaurants where the tag is used,
/// and only the legend category changes while item references remain intact.
/// Demonstrates idempotency by ensuring a second drain produces no changes.
/// </summary>
public class TagCategoryChangedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task TagCategoryChanged_ShouldRebuildFullMenuView_AndUpdateLegendCategory_AndBeIdempotent()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = TestData.DefaultRestaurantId;

        var tagResult = Tag.Create("Spicy", TagCategory.SpiceLevel);
        tagResult.IsSuccess.Should().BeTrue();
        var tag = tagResult.Value;
        tag.ClearDomainEvents();
        await AddAsync(tag);

        var itemId = TestData.GetMenuItemId(Testing.TestData.MenuItems.BuffaloWings);
        // Critical: assignment ensures the tag is picked up by FullMenuViewRebuilder.
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
            tagJson.GetProperty("name").GetString().Should().Be("Spicy");
            tagJson.GetProperty("category").GetString().Should().Be(TagCategory.SpiceLevel.ToString());
        }

        // Act: change category -> emits TagCategoryChanged; legend category should reflect the new category.
        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var t = await db.Tags.FirstAsync(x => x.Id == tag.Id);
            var upd = t.ChangeCategory(TagCategory.Cuisine);
            upd.IsSuccess.Should().BeTrue();
            await db.SaveChangesAsync();
        });

        await DrainOutboxAsync();

        // Assert first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.Tags.EventHandlers.TagCategoryChangedEventHandler).FullName!;
            (await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync()).Should().HaveCount(1);

            var processedOutbox = await db.Set<OutboxMessage>().Where(m => m.Type.Contains("TagCategoryChanged")).ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

            var view = await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId);
            view.LastRebuiltAt.Should().BeAfter(baselineRebuiltAt);

            var root = FullMenuViewAssertions.GetRoot(view);
            var legend = root.GetProperty("tagLegend").GetProperty("byId");
            legend.TryGetProperty(tag.Id.Value.ToString(), out var tagJson).Should().BeTrue();
            tagJson.GetProperty("name").GetString().Should().Be("Spicy");
            tagJson.GetProperty("category").GetString().Should().Be(TagCategory.Cuisine.ToString());
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
            var handlerName = typeof(YummyZoom.Application.Tags.EventHandlers.TagCategoryChangedEventHandler).FullName!;
            (await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync()).Should().HaveCount(1);
            (await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId)).LastRebuiltAt.Should().Be(afterFirst);
        }
    }
}
