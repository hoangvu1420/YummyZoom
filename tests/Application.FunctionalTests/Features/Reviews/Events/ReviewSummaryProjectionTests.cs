using System;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.ReviewAggregate;
using YummyZoom.Domain.ReviewAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Models;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;
using Address = YummyZoom.Domain.RestaurantAggregate.ValueObjects.Address;
using Rating = YummyZoom.Domain.ReviewAggregate.ValueObjects.Rating;

namespace YummyZoom.Application.FunctionalTests.Features.Reviews.Events;

[TestFixture]
public sealed class ReviewSummaryProjectionTests : BaseTestFixture
{
    [Test]
    public async Task ReviewCreated_UpdatesSummaryAndSearchIndex()
    {
        var restaurantId = await CreateRestaurantAsync("Ratings R", "Cafe", null, null);
        await DrainOutboxAsync();

        // Initially, no summary row; search index should have null AvgRating and 0 ReviewCount
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var idx = await db.SearchIndexItems.FindAsync(restaurantId);
            idx.Should().NotBeNull();
            idx!.AvgRating.Should().BeNull();
            idx.ReviewCount.Should().Be(0);
        }

        // Add first review (4)
        var r1 = await CreateReviewAsync(restaurantId, 4);
        await DrainOutboxAsync();

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var summary = await db.RestaurantReviewSummaries.FindAsync(restaurantId);
            summary.Should().NotBeNull();
            summary!.AverageRating.Should().BeApproximately(4.0, 1e-6);
            summary.TotalReviews.Should().Be(1);

            var idx = await db.SearchIndexItems.FindAsync(restaurantId);
            idx!.AvgRating.Should().BeApproximately(4.0, 1e-6);
            idx.ReviewCount.Should().Be(1);
        }

        // Add second review (5)
        var r2 = await CreateReviewAsync(restaurantId, 5);
        await DrainOutboxAsync();

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var summary = await db.RestaurantReviewSummaries.FindAsync(restaurantId);
            summary!.AverageRating.Should().BeApproximately(4.5, 1e-6);
            summary.TotalReviews.Should().Be(2);

            var idx = await db.SearchIndexItems.FindAsync(restaurantId);
            idx!.AvgRating.Should().BeApproximately(4.5, 1e-6);
            idx.ReviewCount.Should().Be(2);
        }
    }

    [Test]
    public async Task ReviewHidden_UpdatesSummaryAndSearchIndex()
    {
        var restaurantId = await CreateRestaurantAsync("Hide R", "Cafe", null, null);
        await DrainOutboxAsync();

        var rev1 = await CreateReviewAsync(restaurantId, 4);
        var rev2 = await CreateReviewAsync(restaurantId, 2);
        await DrainOutboxAsync();

        // Baseline: avg = 3.0, count = 2
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var summary = await db.RestaurantReviewSummaries.FindAsync(restaurantId);
            summary!.AverageRating.Should().BeApproximately(3.0, 1e-6);
            summary.TotalReviews.Should().Be(2);
        }

        // Hide the 2-star review
        var review = await FindAsync<Review>(rev2);
        review!.Hide();
        await UpdateAsync(review);
        await DrainOutboxAsync();

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var summary = await db.RestaurantReviewSummaries.FindAsync(restaurantId);
            summary!.AverageRating.Should().BeApproximately(4.0, 1e-6);
            summary.TotalReviews.Should().Be(1);

            var idx = await db.SearchIndexItems.FindAsync(restaurantId);
            idx!.AvgRating.Should().BeApproximately(4.0, 1e-6);
            idx.ReviewCount.Should().Be(1);
        }
    }

    [Test]
    public async Task ReviewDeleted_UpdatesSummaryAndSearchIndex()
    {
        var restaurantId = await CreateRestaurantAsync("Delete R", "Cafe", null, null);
        await DrainOutboxAsync();

        var rev1 = await CreateReviewAsync(restaurantId, 4);
        var rev2 = await CreateReviewAsync(restaurantId, 5);
        await DrainOutboxAsync();

        // Delete the 4-star review
        var review = await FindAsync<Review>(rev1);
        review!.MarkAsDeleted(DateTimeOffset.UtcNow);
        await UpdateAsync(review);
        await DrainOutboxAsync();

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var summary = await db.RestaurantReviewSummaries.FindAsync(restaurantId);
            summary!.AverageRating.Should().BeApproximately(5.0, 1e-6);
            summary.TotalReviews.Should().Be(1);

            var idx = await db.SearchIndexItems.FindAsync(restaurantId);
            idx!.AvgRating.Should().BeApproximately(5.0, 1e-6);
            idx.ReviewCount.Should().Be(1);
        }
    }

    private static async Task<Guid> CreateRestaurantAsync(string name, string cuisine, double? lat, double? lon)
    {
        var address = Address.Create("1 St", "C", "S", "Z", "US").Value;
        var contact = ContactInfo.Create("+1-555-0123", "t@test.local").Value;
        var hours = BusinessHours.Create("9-5").Value;
        var created = Restaurant.Create(name, null, null, "desc", cuisine, address, contact, hours);
        var entity = created.Value;

        if (lat.HasValue && lon.HasValue)
        {
            entity.ChangeGeoCoordinates(lat.Value, lon.Value);
        }

        entity.Verify();
        entity.AcceptOrders();

        await AddAsync(entity);
        return entity.Id.Value;
    }

    private static async Task<ReviewId> CreateReviewAsync(Guid restaurantId, int rating)
    {
        var reviewResult = Review.Create(
            OrderId.CreateUnique(),
            UserId.CreateUnique(),
            RestaurantId.Create(restaurantId),
            Rating.Create(rating).Value,
            "test");

        var review = reviewResult.Value;
        await AddAsync(review);
        return review.Id;
    }
}
