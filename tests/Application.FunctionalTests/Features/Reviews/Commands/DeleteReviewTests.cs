using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;
using YummyZoom.Application.Reviews.Commands.CreateReview;
using YummyZoom.Application.Reviews.Commands.DeleteReview;
using YummyZoom.Domain.ReviewAggregate;
using YummyZoom.Domain.ReviewAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Reviews.Commands;

public class DeleteReviewTests : BaseTestFixture
{
    private static async Task<Guid> CreateDeliveredOrderAsync()
    {
        var customerId = await RunAsDefaultUserAsync();
        SetUserId(customerId);
        var init = InitiateOrderTestHelper.BuildValidCommand(customerId: customerId, paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery);
        var initResult = await SendAsync(init);
        initResult.ShouldBeSuccessful();
        var orderId = initResult.Value.OrderId;

        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        await OrderLifecycleTestHelper.AcceptAsync(orderId, DateTime.UtcNow.AddMinutes(30));
        await OrderLifecycleTestHelper.MarkPreparingAsync(orderId);
        await OrderLifecycleTestHelper.MarkReadyAsync(orderId);
        await OrderLifecycleTestHelper.MarkDeliveredAsync(orderId);

        await RunAsDefaultUserAsync();
        return orderId.Value;
    }

    [Test]
    public async Task DeleteReview_AsOwner_ShouldRemoveFromReads()
    {
        // Arrange: create review
        var orderId = await CreateDeliveredOrderAsync();
        var create = await SendAsync(new CreateReviewCommand(orderId, Testing.TestData.DefaultRestaurantId, 5, null, "nice")
        { UserId = UserId.Create(GetUserId()!.Value) });
        create.ShouldBeSuccessful();
        var reviewId = ReviewId.Create(create.Value.ReviewId);
        (await FindAsync<Review>(reviewId)).Should().NotBeNull();

        // Act: delete
        var delete = new DeleteReviewCommand(reviewId.Value) { UserId = UserId.Create(GetUserId()!.Value) };
        var result = await SendAsync(delete);

        // Assert: soft deleted so EF global filter hides it
        result.ShouldBeSuccessful();
        var after = await FindAsync<Review>(reviewId);
        after.Should().BeNull();
    }

    [Test]
    public async Task DeleteReview_WhenNotOwner_ShouldFailNotOwner()
    {
        // Arrange: create review as default user
        var orderId = await CreateDeliveredOrderAsync();
        var create = await SendAsync(new CreateReviewCommand(orderId, Testing.TestData.DefaultRestaurantId, 4, null, "ok")
        { UserId = UserId.Create(GetUserId()!.Value) });
        create.ShouldBeSuccessful();
        var reviewId = create.Value.ReviewId;

        // Switch to different user
        var otherUserId = await RunAsUserAsync("deleter@yummyzoom.test", "P@ssw0rd!", new[] { YummyZoom.SharedKernel.Constants.Roles.User });

        // Act
        var result = await SendAsync(new DeleteReviewCommand(reviewId)
        {
            UserId = UserId.Create(otherUserId)
        });

        // Assert (business rule enforced in handler)
        result.ShouldBeFailure("DeleteReview.NotOwner");
    }

    [Test]
    public async Task DeleteReview_WhenNotFound_ShouldReturnNotFound()
    {
        await RunAsDefaultUserAsync();
        var cmd = new DeleteReviewCommand(Guid.NewGuid())
        {
            UserId = UserId.Create(GetUserId()!.Value)
        };
        var result = await SendAsync(cmd);
        result.ShouldBeFailure("DeleteReview.NotFound");
    }
}
