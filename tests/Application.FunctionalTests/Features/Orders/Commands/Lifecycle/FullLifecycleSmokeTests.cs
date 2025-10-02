using YummyZoom.Domain.OrderAggregate.Enums;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;

/// <summary>
/// Full lifecycle smoke test: Placed → Accepted → Preparing → Ready → Delivered in one flow.
/// Verifies the complete order fulfillment chain works end-to-end.
/// </summary>
public class FullLifecycleSmokeTests : OrderLifecycleTestBase
{
    [Test]
    public async Task FullLifecycle_AsStaff_ShouldReachDelivered()
    {
        // Arrange - start with placed order and switch to staff context
        var orderId = await OrderLifecycleTestHelper.CreatePlacedOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var eta = DateTime.UtcNow.AddMinutes(30);

        // Verify initial state
        var placedOrder = await FindOrderAsync(orderId);
        placedOrder!.Status.Should().Be(OrderStatus.Placed);

        // Act & Assert: Placed → Accepted
        var acceptResult = await OrderLifecycleTestHelper.AcceptAsync(orderId, eta);
        acceptResult.IsSuccess.Should().BeTrue();
        var acceptedOrder = await FindOrderAsync(orderId);
        acceptedOrder!.Status.Should().Be(OrderStatus.Accepted);
        acceptedOrder.EstimatedDeliveryTime.Should().NotBeNull();

        // Act & Assert: Accepted → Preparing
        var preparingResult = await OrderLifecycleTestHelper.MarkPreparingAsync(orderId);
        preparingResult.IsSuccess.Should().BeTrue();
        var preparingOrder = await FindOrderAsync(orderId);
        preparingOrder!.Status.Should().Be(OrderStatus.Preparing);

        // Act & Assert: Preparing → Ready
        var readyResult = await OrderLifecycleTestHelper.MarkReadyAsync(orderId);
        readyResult.IsSuccess.Should().BeTrue();
        var readyOrder = await FindOrderAsync(orderId);
        readyOrder!.Status.Should().Be(OrderStatus.ReadyForDelivery);

        // Act & Assert: Ready → Delivered
        var deliveredResult = await OrderLifecycleTestHelper.MarkDeliveredAsync(orderId, DateTime.UtcNow);
        deliveredResult.IsSuccess.Should().BeTrue();
        var deliveredOrder = await FindOrderAsync(orderId);
        deliveredOrder!.Status.Should().Be(OrderStatus.Delivered);
        deliveredOrder.ActualDeliveryTime.Should().NotBeNull();

        // Final verification: ensure delivery time is recorded
        deliveredOrder.ActualDeliveryTime.Should().NotBeNull();
        deliveredOrder.LastUpdateTimestamp.Should().BeAfter(placedOrder.LastUpdateTimestamp);
    }

    [Test]
    public async Task FullLifecycle_WithCancellationMidway_ShouldStopAtCancelled()
    {
        // Arrange - start with accepted order
        var orderId = await OrderLifecycleTestHelper.CreateAcceptedOrderAsync();
        var staffUserId = await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();

        // Verify accepted state
        var acceptedOrder = await FindOrderAsync(orderId);
        acceptedOrder!.Status.Should().Be(OrderStatus.Accepted);

        // Act: Move to Preparing
        var preparingResult = await OrderLifecycleTestHelper.MarkPreparingAsync(orderId);
        preparingResult.IsSuccess.Should().BeTrue();

        // Act: Cancel while preparing (staff has authority)
        var cancelResult = await OrderLifecycleTestHelper.CancelAsync(orderId, staffUserId, "Kitchen overloaded");
        cancelResult.IsSuccess.Should().BeTrue();

        // Assert: Order is cancelled and further transitions should fail
        var cancelledOrder = await FindOrderAsync(orderId);
        cancelledOrder!.Status.Should().Be(OrderStatus.Cancelled);

        // Verify that attempting to continue lifecycle fails
        var readyResult = await OrderLifecycleTestHelper.MarkReadyAsync(orderId);
        readyResult.IsFailure.Should().BeTrue();

        // Status should remain cancelled
        var stillCancelledOrder = await FindOrderAsync(orderId);
        stillCancelledOrder!.Status.Should().Be(OrderStatus.Cancelled);
    }
}
