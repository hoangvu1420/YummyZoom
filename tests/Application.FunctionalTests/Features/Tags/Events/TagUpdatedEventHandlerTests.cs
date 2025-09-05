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
/// Validates TagUpdated triggers a targeted FullMenuView rebuild only for restaurants
/// that have menu items referencing the tag via DietaryTagIds. The test assigns the
/// tag to an existing item to ensure it participates in the FullMenuView build.
/// Also verifies inbox idempotency and JSON legend updates.
/// </summary>
public class TagUpdatedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task TagUpdated_ShouldRebuildFullMenuView_AndUpdateTagLegend_AndBeIdempotent()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = TestData.DefaultRestaurantId;

        // Create a tag and persist without emitting TagCreated (clear events)
        // Rationale: We want a stable baseline view first; TagCreated is tested separately as a no-op.
        var tagResult = Tag.Create("Vegan", TagCategory.Dietary);
        tagResult.IsSuccess.Should().BeTrue();
        var tag = tagResult.Value;
        tag.ClearDomainEvents();
        await AddAsync(tag);

        // Critical: Assign tag to an existing item so the tag is included in FullMenuView.
        // The rebuilder collects tag legend entries only from tags referenced by items.
        var itemId = TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await MenuTestDataFactory.AttachTagsToItemAsync(itemId, new List<Guid> { tag.Id.Value });

        // Baseline: rebuild and capture initial state to compare LastRebuiltAt and legend before update.
        await SendAsync(new YummyZoom.Application.Admin.Commands.RebuildFullMenu.RebuildFullMenuCommand(restaurantId));

        DateTimeOffset baselineRebuiltAt;
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var view = await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId);
            baselineRebuiltAt = view.LastRebuiltAt;

            // Pre-condition: tag legend contains the tag with original name/category
            var root = FullMenuViewAssertions.GetRoot(view);
            var legend = root.GetProperty("tagLegend").GetProperty("byId");
            legend.TryGetProperty(tag.Id.Value.ToString(), out var tagJson).Should().BeTrue();
            tagJson.GetProperty("name").GetString().Should().Be("Vegan");
            tagJson.GetProperty("category").GetString().Should().Be(TagCategory.Dietary.ToString());
        }

        // Act: update tag name -> emits TagUpdated and should trigger rebuild for the affected restaurant.
        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var t = await db.Tags.FirstAsync(x => x.Id == tag.Id);
            var upd = t.UpdateDetails("Plant-based", null);
            upd.IsSuccess.Should().BeTrue();
            await db.SaveChangesAsync();
        });

        await DrainOutboxAsync();

        // Assert first drain
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.Tags.EventHandlers.TagUpdatedEventHandler).FullName!;
            (await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync()).Should().HaveCount(1);

            var processedOutbox = await db.Set<OutboxMessage>().Where(m => m.Type.Contains("TagUpdated")).ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

            var view = await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId);
            view.LastRebuiltAt.Should().BeAfter(baselineRebuiltAt);

            var root = FullMenuViewAssertions.GetRoot(view);
            var legend = root.GetProperty("tagLegend").GetProperty("byId");
            legend.TryGetProperty(tag.Id.Value.ToString(), out var tagJson).Should().BeTrue();
            tagJson.GetProperty("name").GetString().Should().Be("Plant-based");
            tagJson.GetProperty("category").GetString().Should().Be(TagCategory.Dietary.ToString());
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
            var handlerName = typeof(YummyZoom.Application.Tags.EventHandlers.TagUpdatedEventHandler).FullName!;
            (await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync()).Should().HaveCount(1);
            (await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId)).LastRebuiltAt.Should().Be(afterFirst);
        }
    }
}
