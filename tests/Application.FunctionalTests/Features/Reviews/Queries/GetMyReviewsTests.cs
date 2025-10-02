using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;
using YummyZoom.Application.Reviews.Commands.CreateReview;
using YummyZoom.Application.Reviews.Queries.GetMyReviews;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Reviews.Queries;

public class GetMyReviewsTests : BaseTestFixture
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
    public async Task GetMine_ShouldReturnOwnReviews_NewestFirst()
    {
        var userId = await RunAsDefaultUserAsync();
        var o1 = await CreateDeliveredOrderForAsync(userId);
        await RunAsDefaultUserAsync();
        await SendAsync(new CreateReviewCommand(o1, Testing.TestData.DefaultRestaurantId, 4, null, "r1")
        { UserId = YummyZoom.Domain.UserAggregate.ValueObjects.UserId.Create(userId) });

        // Create a second restaurant and order there to avoid duplicate-per-restaurant restriction
        var (restaurant2Id, menuItemId) = await TestData.TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        await Task.Delay(10);
        // Place and deliver order at the second restaurant
        SetUserId(userId);
        var cmd2 = InitiateOrderTestHelper.BuildValidCommand(customerId: userId, restaurantId: restaurant2Id, paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery, menuItemIds: new List<Guid> { menuItemId });
        var r2 = await SendAsync(cmd2);
        r2.ShouldBeSuccessful();
        var o2 = r2.Value.OrderId;
        // Advance status for restaurant2: run as its staff and issue commands with restaurant2Id
        var staff2 = await RunAsRestaurantStaffAsync("staff2@yummyzoom.test", restaurant2Id);
        await SendAsync(new YummyZoom.Application.Orders.Commands.AcceptOrder.AcceptOrderCommand(o2.Value, restaurant2Id, DateTime.UtcNow.AddMinutes(25)));
        await SendAsync(new YummyZoom.Application.Orders.Commands.MarkOrderPreparing.MarkOrderPreparingCommand(o2.Value, restaurant2Id));
        await SendAsync(new YummyZoom.Application.Orders.Commands.MarkOrderReadyForDelivery.MarkOrderReadyForDeliveryCommand(o2.Value, restaurant2Id));
        await SendAsync(new YummyZoom.Application.Orders.Commands.MarkOrderDelivered.MarkOrderDeliveredCommand(o2.Value, restaurant2Id, DateTime.UtcNow));
        await RunAsDefaultUserAsync();
        await SendAsync(new CreateReviewCommand(o2.Value, restaurant2Id, 5, null, "r2")
        { UserId = YummyZoom.Domain.UserAggregate.ValueObjects.UserId.Create(userId) });

        var page = await SendAndUnwrapAsync(new GetMyReviewsQuery(1, 10));
        page.Items.Should().HaveCountGreaterThanOrEqualTo(2);
        page.Items.First().Comment.Should().Be("r2");
    }

    [Test]
    public async Task GetMine_WhenUnauthenticated_ShouldThrowUnauthorized()
    {
        SetUserId(null);
        var act = async () => await SendAsync(new GetMyReviewsQuery(1, 10));
        await act.Should().ThrowAsync<System.UnauthorizedAccessException>();
    }
}
