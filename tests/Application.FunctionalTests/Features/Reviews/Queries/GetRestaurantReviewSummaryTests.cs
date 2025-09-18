using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;
using YummyZoom.Application.Reviews.Commands.CreateReview;
using YummyZoom.Application.Reviews.Queries.GetRestaurantReviewSummary;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Reviews.Queries;

public class GetRestaurantReviewSummaryTests : BaseTestFixture
{
    private static async Task<Guid> CreateDeliveredOrderForAsync(Guid userId)
    {
        SetUserId(userId);
        var cmd = InitiateOrderTestHelper.BuildValidCommand(customerId: userId, paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery);
        var r = await SendAsync(cmd);
        r.ShouldBeSuccessful();
        var orderId = r.Value.OrderId;
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        await OrderLifecycleTestHelper.AcceptAsync(orderId, DateTime.UtcNow.AddMinutes(25));
        await OrderLifecycleTestHelper.MarkPreparingAsync(orderId);
        await OrderLifecycleTestHelper.MarkReadyAsync(orderId);
        await OrderLifecycleTestHelper.MarkDeliveredAsync(orderId);
        return orderId.Value;
    }

    [Test]
    public async Task Summary_ShouldReflectAverageAndTotal()
    {
        // Arrange: two users submit reviews
        var u1 = await RunAsDefaultUserAsync();
        var o1 = await CreateDeliveredOrderForAsync(u1);
        await RunAsDefaultUserAsync();
        await SendAsync(new CreateReviewCommand(o1, Testing.TestData.DefaultRestaurantId, 5, null, "great")
        { UserId = YummyZoom.Domain.UserAggregate.ValueObjects.UserId.Create(u1) });

        var u2 = await RunAsUserAsync("sum2@yummyzoom.test", "P@ssw0rd!", new[] { YummyZoom.SharedKernel.Constants.Roles.User });
        var o2 = await CreateDeliveredOrderForAsync(u2);
        await RunAsUserAsync("sum2@yummyzoom.test", "P@ssw0rd!", new[] { YummyZoom.SharedKernel.Constants.Roles.User });
        await SendAsync(new CreateReviewCommand(o2, Testing.TestData.DefaultRestaurantId, 3, null, "ok")
        { UserId = YummyZoom.Domain.UserAggregate.ValueObjects.UserId.Create(u2) });

        // Project
        await DrainOutboxAsync();

        // Act
        var summary = await SendAndUnwrapAsync(new GetRestaurantReviewSummaryQuery(Testing.TestData.DefaultRestaurantId));

        // Assert: current maintainer updates AverageRating and TotalReviews; distribution fields filled in later step
        summary.TotalReviews.Should().BeGreaterThanOrEqualTo(2);
        summary.AverageRating.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task Summary_ShouldComputeDistribution_TextCounts_AndLastReview()
    {
        // Arrange: two users submit two reviews with different ratings and text presence
        var u1 = await RunAsDefaultUserAsync();
        var o1 = await CreateDeliveredOrderForAsync(u1);
        await RunAsDefaultUserAsync();
        await SendAsync(new CreateReviewCommand(o1, Testing.TestData.DefaultRestaurantId, 5, null, "has text")
        { UserId = YummyZoom.Domain.UserAggregate.ValueObjects.UserId.Create(u1) });

        var u2 = await RunAsUserAsync("dist2@yummyzoom.test", "P@ssw0rd!", new[] { YummyZoom.SharedKernel.Constants.Roles.User });
        var o2 = await CreateDeliveredOrderForAsync(u2);
        await RunAsUserAsync("dist2@yummyzoom.test", "P@ssw0rd!", new[] { YummyZoom.SharedKernel.Constants.Roles.User });
        await Task.Delay(10); // ensure last timestamp is from this review
        await SendAsync(new CreateReviewCommand(o2, Testing.TestData.DefaultRestaurantId, 3, null, null)
        { UserId = YummyZoom.Domain.UserAggregate.ValueObjects.UserId.Create(u2) });

        await DrainOutboxAsync();

        // Act
        var summary = await SendAndUnwrapAsync(new GetRestaurantReviewSummaryQuery(Testing.TestData.DefaultRestaurantId));

        // Assert distribution
        summary.TotalReviews.Should().BeGreaterThanOrEqualTo(2);
        summary.Ratings5.Should().BeGreaterThanOrEqualTo(1);
        summary.Ratings3.Should().BeGreaterThanOrEqualTo(1);
        (summary.Ratings1 + summary.Ratings2 + summary.Ratings3 + summary.Ratings4 + summary.Ratings5)
            .Should().Be(summary.TotalReviews);

        // Text counts and last timestamp
        summary.TotalWithText.Should().BeGreaterThanOrEqualTo(1);
        summary.LastReviewAtUtc.Should().NotBeNull();
        summary.UpdatedAtUtc.Should().BeAfter(DateTime.UtcNow.AddMinutes(-10));
    }

    [Test]
    public async Task Summary_AfterDelete_ShouldDecrementCounts()
    {
        // Arrange: create two reviews
        var u1 = await RunAsDefaultUserAsync();
        var o1 = await CreateDeliveredOrderForAsync(u1);
        await RunAsDefaultUserAsync();
        var r1 = await SendAsync(new CreateReviewCommand(o1, Testing.TestData.DefaultRestaurantId, 4, null, "first")
        { UserId = YummyZoom.Domain.UserAggregate.ValueObjects.UserId.Create(u1) });
        r1.ShouldBeSuccessful();

        var u2 = await RunAsUserAsync("del2@yummyzoom.test", "P@ssw0rd!", new[] { YummyZoom.SharedKernel.Constants.Roles.User });
        var o2 = await CreateDeliveredOrderForAsync(u2);
        await RunAsUserAsync("del2@yummyzoom.test", "P@ssw0rd!", new[] { YummyZoom.SharedKernel.Constants.Roles.User });
        var r2 = await SendAsync(new CreateReviewCommand(o2, Testing.TestData.DefaultRestaurantId, 5, null, "second")
        { UserId = YummyZoom.Domain.UserAggregate.ValueObjects.UserId.Create(u2) });
        r2.ShouldBeSuccessful();

        await DrainOutboxAsync();
        var before = await SendAndUnwrapAsync(new GetRestaurantReviewSummaryQuery(Testing.TestData.DefaultRestaurantId));

        // Act: delete the second review as its owner
        var reviewId = r2.Value.ReviewId;
        var del = await SendAsync(new YummyZoom.Application.Reviews.Commands.DeleteReview.DeleteReviewCommand(reviewId)
        { UserId = YummyZoom.Domain.UserAggregate.ValueObjects.UserId.Create(u2) });
        del.ShouldBeSuccessful();

        await DrainOutboxAsync();
        var after = await SendAndUnwrapAsync(new GetRestaurantReviewSummaryQuery(Testing.TestData.DefaultRestaurantId));

        // Assert: counts reduced appropriately
        after.TotalReviews.Should().Be(before.TotalReviews - 1);
        after.Ratings5.Should().Be(before.Ratings5 - 1);
    }
}
