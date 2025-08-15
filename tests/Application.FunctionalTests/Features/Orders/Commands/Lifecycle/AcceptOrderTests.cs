using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Orders.Commands.AcceptOrder;
using YummyZoom.Domain.OrderAggregate.Enums;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;

/// <summary>
/// AcceptOrder command tests: happy path, idempotency, and authorization failure.
/// </summary>
public class AcceptOrderTests : OrderLifecycleTestBase
{
    private static DateTime DefaultEta() => DateTime.UtcNow.AddMinutes(25);

    [Test]
    public async Task Accept_AsStaff_ShouldSetStatusAccepted()
    {
        // Arrange
        var placedOrderId = await OrderLifecycleTestHelper.CreatePlacedOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var eta = DefaultEta();

        // Act
        var result =
            await SendAsync(new AcceptOrderCommand(placedOrderId.Value, Testing.TestData.DefaultRestaurantId, eta));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrderStatus.Accepted.ToString());
        result.Value.EstimatedDeliveryTime.Should().NotBeNull();

        var order = await FindOrderAsync(placedOrderId);
        order!.Status.Should().Be(OrderStatus.Accepted);
        order.EstimatedDeliveryTime.Should().NotBeNull();
    }

    [Test]
    public async Task Accept_WhenAlreadyAccepted_ShouldBeIdempotent()
    {
        // Arrange
        var orderId = await OrderLifecycleTestHelper.CreateAcceptedOrderAsync(DefaultEta());
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var firstOrder = await FindOrderAsync(orderId);
        firstOrder!.Status.Should().Be(OrderStatus.Accepted);
        var firstEta = firstOrder.EstimatedDeliveryTime;
        var firstLastUpdate = firstOrder.LastUpdateTimestamp;

        // Act
        var newEta = DateTime.UtcNow.AddMinutes(40); // attempt to change ETA
        var result =
            await SendAsync(new AcceptOrderCommand(orderId.Value, Testing.TestData.DefaultRestaurantId, newEta));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrderStatus.Accepted.ToString());

        var afterOrder = await FindOrderAsync(orderId);
        afterOrder!.Status.Should().Be(OrderStatus.Accepted);
        afterOrder.EstimatedDeliveryTime.Should().Be(firstEta); // expecting idempotent no-change
        afterOrder.LastUpdateTimestamp.Should().Be(firstLastUpdate);
    }

    [Test]
    public async Task Accept_AsCustomer_ShouldFail()
    {
        // Arrange (customer context already default)
        var orderId = await OrderLifecycleTestHelper.CreatePlacedOrderAsync();
        var eta = DefaultEta();

        // Act
        var act = async () => await SendAsync(new AcceptOrderCommand(orderId.Value, Testing.TestData.DefaultRestaurantId, eta));

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
        var order = await FindOrderAsync(orderId);
        order!.Status.Should().Be(OrderStatus.Placed);
    }
}
