using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Orders.Commands.MarkOrderDelivered;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;

/// <summary>
/// MarkOrderDelivered command tests: happy path, invalid transition, idempotency, auth failure.
/// </summary>
public class MarkOrderDeliveredTests : OrderLifecycleTestBase
{
    [Test]
    public async Task Delivered_AsStaff_FromReady_ShouldSucceed()
    {
        // Arrange
        var orderId = await OrderLifecycleTestHelper.CreateReadyOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var deliveredAt = DateTime.UtcNow;

        // Act
        var result = await SendAsync(new MarkOrderDeliveredCommand(orderId.Value, Testing.TestData.DefaultRestaurantId, deliveredAt));

        // Assert
        result.ShouldBeSuccessful();
        var order = await FindOrderAsync(orderId);
        order!.Status.Should().Be(OrderStatus.Delivered);
        order.ActualDeliveryTime.Should().NotBeNull();
    }

    [Test]
    public async Task Delivered_FromPreparing_ShouldFail()
    {
        // Arrange preparing order
        var orderId = await OrderLifecycleTestHelper.CreatePreparingOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();

        // Act
        var result = await SendAsync(new MarkOrderDeliveredCommand(orderId.Value, Testing.TestData.DefaultRestaurantId, DateTime.UtcNow));

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(OrderErrors.InvalidOrderStatusForDelivered);
        var order = await FindOrderAsync(orderId);
        order!.Status.Should().Be(OrderStatus.Preparing);
    }

    [Test]
    public async Task Delivered_IdempotentSecondCall_ShouldRemainDelivered()
    {
        // Arrange deliver order first
        var orderId = await OrderLifecycleTestHelper.CreateReadyOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var firstResult = await SendAsync(new MarkOrderDeliveredCommand(orderId.Value, Testing.TestData.DefaultRestaurantId, DateTime.UtcNow));
        firstResult.ShouldBeSuccessful();
        var first = await FindOrderAsync(orderId);
        first!.Status.Should().Be(OrderStatus.Delivered);
        var firstTimestamp = first.LastUpdateTimestamp;
        var firstDeliveredAt = first.ActualDeliveryTime;

        // Act second (idempotent) call with different timestamp attempt
        var second = await SendAsync(new MarkOrderDeliveredCommand(orderId.Value, Testing.TestData.DefaultRestaurantId, DateTime.UtcNow.AddMinutes(5)));

        // Assert
        second.ShouldBeSuccessful();
        var after = await FindOrderAsync(orderId);
        after!.Status.Should().Be(OrderStatus.Delivered);
        after.LastUpdateTimestamp.Should().Be(firstTimestamp);
        after.ActualDeliveryTime.Should().Be(firstDeliveredAt);
    }

    [Test]
    public async Task Delivered_AsCustomer_ShouldFail()
    {
        // Arrange ready order but stay as customer
        var orderId = await OrderLifecycleTestHelper.CreateReadyOrderAsync();

        // Switch back to customer context to test authorization failure
        SetUserId(Testing.TestData.DefaultCustomerId);

        // Act
        var act = async () => await SendAsync(new MarkOrderDeliveredCommand(orderId.Value, Testing.TestData.DefaultRestaurantId, DateTime.UtcNow));

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
        var order = await FindOrderAsync(orderId);
        order!.Status.Should().Be(OrderStatus.ReadyForDelivery);
    }
}
