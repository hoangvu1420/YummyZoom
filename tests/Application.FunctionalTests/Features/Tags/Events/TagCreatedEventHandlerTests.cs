using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Domain.TagEntity;
using YummyZoom.Domain.TagEntity.Enums;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Models;
using YummyZoom.Infrastructure.Data.ReadModels.FullMenu;

namespace YummyZoom.Application.FunctionalTests.Features.Tags.Events;

using static Testing;

/// <summary>
/// Verifies TagCreated handler is effectively a no-op for FullMenuView when no items reference the new tag.
/// - Important: FullMenuViewRebuilder only includes tags that are referenced by items (via MenuItems.DietaryTagIds).
/// - Therefore, creating a tag alone must not change the FullMenuView for any restaurant.
/// - We still expect inbox idempotency and processed outbox records for the TagCreated event.
/// </summary>
public class TagCreatedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task TagCreated_ShouldNotRebuildFullMenuView_WhenUnusedTag_AndBeIdempotent()
    {
        // Arrange: ensure a baseline FullMenuView exists to compare LastRebuiltAt.
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", TestData.DefaultRestaurantId);
        var restaurantId = TestData.DefaultRestaurantId;

        // Force a baseline rebuild so there is a FullMenuView row and we capture LastRebuiltAt.
        await SendAsync(new YummyZoom.Application.Admin.Commands.RebuildFullMenu.RebuildFullMenuCommand(restaurantId));

        DateTimeOffset baselineRebuiltAt;
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var view = await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId);
            baselineRebuiltAt = view.LastRebuiltAt;
        }

        // Act: Create a new tag (emits TagCreated). No items reference it.
        var tagResult = Tag.Create("Brand New Tag", TagCategory.Dietary);
        tagResult.IsSuccess.Should().BeTrue();
        var tag = tagResult.Value; // keep domain events to enqueue outbox
        await AddAsync(tag);

        await DrainOutboxAsync();

        // Assert: inbox idempotency + no changes to FullMenuView (LastRebuiltAt unchanged).
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.Tags.EventHandlers.TagCreatedEventHandler).FullName!;

            var inboxEntries = await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync();
            inboxEntries.Should().HaveCount(1, "TagCreated should be handled once (idempotent)");

            var processedOutbox = await db.Set<OutboxMessage>().Where(m => m.Type.Contains("TagCreated")).ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

            var view = await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId);
            view.LastRebuiltAt.Should().Be(baselineRebuiltAt, "unused TagCreated should not trigger rebuild");

            // Legend should not include the new tag since no items reference it.
            var root = FullMenuViewAssertions.GetRoot(view);
            var legend = root.GetProperty("tagLegend").GetProperty("byId");
            legend.TryGetProperty(tag.Id.Value.ToString(), out var _).Should().BeFalse();
        }

        // Idempotency: drain again and ensure still no changes.
        await DrainOutboxAsync();
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(YummyZoom.Application.Tags.EventHandlers.TagCreatedEventHandler).FullName!;
            (await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync()).Should().HaveCount(1);

            var view = await db.Set<FullMenuView>().FirstAsync(v => v.RestaurantId == restaurantId);
            view.LastRebuiltAt.Should().Be(baselineRebuiltAt);
        }
    }
}

