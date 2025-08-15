using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Orders.Commands.MarkOrderPreparing;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;

/// <summary>
/// MarkOrderPreparing command tests: happy path, invalid transition, idempotency, auth failure.
/// </summary>
public class MarkOrderPreparingTests : OrderLifecycleTestBase
{
    [Test]
    public async Task Preparing_AsStaff_FromAccepted_ShouldSucceed()
    {
        // Arrange
        var orderId = await OrderLifecycleTestHelper.CreateAcceptedOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();

        // Act
        var result = await SendAsync(new MarkOrderPreparingCommand(orderId.Value, Testing.TestData.DefaultRestaurantId));

        // Assert
        result.ShouldBeSuccessful();
        var order = await FindOrderAsync(orderId);
        order!.Status.Should().Be(OrderStatus.Preparing);
    }

    [Test]
    public async Task Preparing_FromPlaced_ShouldFail()
    {
        // Arrange (placed order, staff context required for command)
        var orderId = await OrderLifecycleTestHelper.CreatePlacedOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();

        // Act
        var result = await SendAsync(new MarkOrderPreparingCommand(orderId.Value, Testing.TestData.DefaultRestaurantId));

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(OrderErrors.InvalidOrderStatusForPreparing);
        var order = await FindOrderAsync(orderId);
        order!.Status.Should().Be(OrderStatus.Placed);
    }

    [Test]
    public async Task Preparing_IdempotentSecondCall_ShouldSucceedAndRemainPreparing()
    {
        // Arrange: move to Preparing
        var orderId = await OrderLifecycleTestHelper.CreatePreparingOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var first = await FindOrderAsync(orderId);
        first!.Status.Should().Be(OrderStatus.Preparing);
        var firstTimestamp = first.LastUpdateTimestamp;

        // Act second call
        var result = await SendAsync(new MarkOrderPreparingCommand(orderId.Value, Testing.TestData.DefaultRestaurantId));

        // Assert
        result.ShouldBeSuccessful();
        var after = await FindOrderAsync(orderId);
        after!.Status.Should().Be(OrderStatus.Preparing);
        after.LastUpdateTimestamp.Should().Be(firstTimestamp);
    }

    [Test]
    public async Task Preparing_AsCustomer_ShouldFail()
    {
        // Arrange accepted order but stay as customer (default context)
        var orderId = await OrderLifecycleTestHelper.CreateAcceptedOrderAsync();

        // Switch back to customer context to test authorization failure
        SetUserId(Testing.TestData.DefaultCustomerId);

        // Act
        var act = async () => await SendAsync(new MarkOrderPreparingCommand(orderId.Value, Testing.TestData.DefaultRestaurantId));

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
        var order = await FindOrderAsync(orderId);
        order!.Status.Should().Be(OrderStatus.Accepted);
    }
}
