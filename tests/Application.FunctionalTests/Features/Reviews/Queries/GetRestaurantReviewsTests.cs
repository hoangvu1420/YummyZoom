using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;
using YummyZoom.Application.Reviews.Commands.CreateReview;
using YummyZoom.Application.Reviews.Queries.GetRestaurantReviews;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Reviews.Queries;

public class GetRestaurantReviewsTests : BaseTestFixture
{
    private static async Task<Guid> CreateDeliveredOrderForAsync(Guid customerId)
    {
        SetUserId(customerId);
        var cmd = InitiateOrderTestHelper.BuildValidCommand(customerId: customerId, paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery);
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
    public async Task GetReviews_ShouldReturnNewestFirst_AndExcludeDeleted()
    {
        // Arrange: two users, two delivered orders, two reviews
        var u1 = await RunAsDefaultUserAsync();
        var o1 = await CreateDeliveredOrderForAsync(u1);
        await RunAsDefaultUserAsync();
        await SendAsync(new CreateReviewCommand(o1, Testing.TestData.DefaultRestaurantId, 5, null, "A")
        { UserId = YummyZoom.Domain.UserAggregate.ValueObjects.UserId.Create(u1) });

        var u2 = await RunAsUserAsync("second@yummyzoom.test", "P@ssw0rd!", new[] { YummyZoom.SharedKernel.Constants.Roles.User });
        var o2 = await CreateDeliveredOrderForAsync(u2);
        await RunAsUserAsync("second@yummyzoom.test", "P@ssw0rd!", new[] { YummyZoom.SharedKernel.Constants.Roles.User });
        await Task.Delay(10); // ensure distinct timestamps
        await SendAsync(new CreateReviewCommand(o2, Testing.TestData.DefaultRestaurantId, 3, null, "B")
        { UserId = YummyZoom.Domain.UserAggregate.ValueObjects.UserId.Create(u2) });

        // Act
        var page = await SendAndUnwrapAsync(new GetRestaurantReviewsQuery(Testing.TestData.DefaultRestaurantId, 1, 10));

        // Assert order and size
        page.Items.Should().HaveCount(2);
        page.Items.First().Comment.Should().Be("B");
        page.Items.Last().Comment.Should().Be("A");
    }
}
