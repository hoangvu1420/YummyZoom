using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Orders.Commands.MarkOrderReadyForDelivery;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;

/// <summary>
/// MarkOrderReadyForDelivery command tests: happy path, invalid transition, idempotency, auth failure.
/// </summary>
public class MarkOrderReadyForDeliveryTests : OrderLifecycleTestBase
{
    [Test]
    public async Task Ready_AsStaff_FromPreparing_ShouldSucceed()
    {
        // Arrange
        var orderId = await OrderLifecycleTestHelper.CreatePreparingOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();

        // Act
        var result = await SendAsync(new MarkOrderReadyForDeliveryCommand(orderId.Value, Testing.TestData.DefaultRestaurantId));

        // Assert
        result.ShouldBeSuccessful();
        var order = await FindOrderAsync(orderId);
        order!.Status.Should().Be(OrderStatus.ReadyForDelivery);
    }

    [Test]
    public async Task Ready_FromAccepted_ShouldFail()
    {
        // Arrange
        var orderId = await OrderLifecycleTestHelper.CreateAcceptedOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();

        // Act
        var result = await SendAsync(new MarkOrderReadyForDeliveryCommand(orderId.Value, Testing.TestData.DefaultRestaurantId));

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(OrderErrors.InvalidOrderStatusForReadyForDelivery);
        var order = await FindOrderAsync(orderId);
        order!.Status.Should().Be(OrderStatus.Accepted);
    }

    [Test]
    public async Task Ready_IdempotentSecondCall_ShouldRemainReady()
    {
        // Arrange
        var orderId = await OrderLifecycleTestHelper.CreateReadyOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var first = await FindOrderAsync(orderId);
        first!.Status.Should().Be(OrderStatus.ReadyForDelivery);
        var firstTimestamp = first.LastUpdateTimestamp;

        // Act
        var result = await SendAsync(new MarkOrderReadyForDeliveryCommand(orderId.Value, Testing.TestData.DefaultRestaurantId));

        // Assert
        result.ShouldBeSuccessful();
        var after = await FindOrderAsync(orderId);
        after!.Status.Should().Be(OrderStatus.ReadyForDelivery);
        after.LastUpdateTimestamp.Should().Be(firstTimestamp);
    }

    [Test]
    public async Task Ready_AsCustomer_ShouldFail()
    {
        // Arrange preparing order but stay as customer
        var orderId = await OrderLifecycleTestHelper.CreatePreparingOrderAsync();

        // Switch back to customer context to test authorization failure
        SetUserId(Testing.TestData.DefaultCustomerId);

        // Act
        var act = async () => await SendAsync(new MarkOrderReadyForDeliveryCommand(orderId.Value, Testing.TestData.DefaultRestaurantId));

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
        var order = await FindOrderAsync(orderId);
        order!.Status.Should().Be(OrderStatus.Preparing);
    }
}
