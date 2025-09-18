using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;
using YummyZoom.Application.Reviews.Commands.CreateReview;
using YummyZoom.Application.Reviews.Queries.GetRestaurantReviewSummary;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.ReviewAggregate;
using YummyZoom.Domain.ReviewAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Reviews.Commands;

public class CreateReviewTests : BaseTestFixture
{
    private static async Task<Guid> CreateDeliveredOrderForUserAsync(Guid customerId)
    {
        // Create placed order for the given customer
        SetUserId(customerId);
        var init = InitiateOrderTestHelper.BuildValidCommand(customerId: customerId, paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery);
        var initResult = await SendAsync(init);
        initResult.ShouldBeSuccessful();
        var orderId = initResult.Value.OrderId;

        // Advance to Delivered as restaurant staff
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        await OrderLifecycleTestHelper.AcceptAsync(orderId, DateTime.UtcNow.AddMinutes(30));
        await OrderLifecycleTestHelper.MarkPreparingAsync(orderId);
        await OrderLifecycleTestHelper.MarkReadyAsync(orderId);
        await OrderLifecycleTestHelper.MarkDeliveredAsync(orderId);

        return orderId.Value;
    }

    [Test]
    public async Task CreateReview_WithDeliveredOrder_ShouldSucceed_AndUpdateSummary()
    {
        // Arrange
        var customerId = await RunAsDefaultUserAsync();
        var orderId = await CreateDeliveredOrderForUserAsync(customerId);

        // Act as the customer
        await RunAsDefaultUserAsync();
        var cmd = new CreateReviewCommand(
            OrderId: orderId,
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Rating: 5,
            Title: "Great!",
            Comment: "Loved it")
        { UserId = UserId.Create(customerId) };

        var result = await SendAsync(cmd);

        // Assert command result
        result.ShouldBeSuccessful();
        var reviewId = ReviewId.Create(result.Value.ReviewId);
        var review = await FindAsync<Review>(reviewId);
        review.Should().NotBeNull();

        // Drain outbox to project summary
        await DrainOutboxAsync();

        var summary = await SendAndUnwrapAsync(new GetRestaurantReviewSummaryQuery(Testing.TestData.DefaultRestaurantId));
        summary.AverageRating.Should().BeGreaterThan(0);
        summary.TotalReviews.Should().Be(1);
    }

    [Test]
    public async Task CreateReview_WhenOrderNotDelivered_ShouldFail()
    {
        // Arrange: create placed order only
        var customerId = await RunAsDefaultUserAsync();
        SetUserId(customerId);
        var init = InitiateOrderTestHelper.BuildValidCommand(customerId: customerId, paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery);
        var initResult = await SendAsync(init);
        initResult.ShouldBeSuccessful();
        var orderId = initResult.Value.OrderId;

        // Ensure auth context
        await RunAsDefaultUserAsync();

        // Act
        var cmd = new CreateReviewCommand(orderId.Value, Testing.TestData.DefaultRestaurantId, 4, null, "ok")
        { UserId = UserId.Create(customerId) };
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeFailure("CreateReview.InvalidOrderStatusForReview");
    }

    [Test]
    public async Task CreateReview_WhenNotOrderOwner_ShouldFail()
    {
        // Arrange: delivered order for default user
        var ownerUserId = await RunAsDefaultUserAsync();
        var orderId = await CreateDeliveredOrderForUserAsync(ownerUserId);

        // Switch to a different authenticated user
        var otherUserId = await RunAsUserAsync("other.user@yummyzoom.test", "P@ssw0rd!", new[] { YummyZoom.SharedKernel.Constants.Roles.User });

        // Act
        var cmd = new CreateReviewCommand(orderId, Testing.TestData.DefaultRestaurantId, 3, null, "meh")
        { UserId = UserId.Create(otherUserId) };
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeFailure("CreateReview.NotOrderOwner");
    }

    [Test]
    public async Task CreateReview_WhenDuplicate_ShouldFail()
    {
        // Arrange
        var customerId = await RunAsDefaultUserAsync();
        var orderId = await CreateDeliveredOrderForUserAsync(customerId);

        await RunAsDefaultUserAsync();
        var first = new CreateReviewCommand(orderId, Testing.TestData.DefaultRestaurantId, 4, null, "good")
        { UserId = UserId.Create(customerId) };
        var r1 = await SendAsync(first);
        r1.ShouldBeSuccessful();

        // Act second attempt by same user/restaurant (same order)
        await RunAsDefaultUserAsync();
        var second = new CreateReviewCommand(orderId, Testing.TestData.DefaultRestaurantId, 5, null, "great")
        { UserId = UserId.Create(customerId) };
        var r2 = await SendAsync(second);

        // Assert
        r2.ShouldBeFailure("CreateReview.ReviewAlreadyExists");
    }
}
