using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Orders.Commands.RejectOrder;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;

/// <summary>
/// RejectOrder command tests: happy path (Placed -> Rejected), invalid transition (from Accepted), and authorization failure (customer attempt).
/// Mirrors structure & style of AcceptOrderTests for consistency.
/// </summary>
public class RejectOrderTests : OrderLifecycleTestBase
{
    [Test]
    public async Task Reject_AsStaff_FromPlaced_ShouldSetStatusRejected()
    {
        // Arrange
        var placedOrderId = await OrderLifecycleTestHelper.CreatePlacedOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();

        // Act
        var result = await SendAsync(new RejectOrderCommand(placedOrderId.Value, Testing.TestData.DefaultRestaurantId, "Out of stock"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrderStatus.Rejected.ToString());

        var order = await FindOrderAsync(placedOrderId);
        order!.Status.Should().Be(OrderStatus.Rejected);
    }

    [Test]
    public async Task Reject_AsStaff_FromAccepted_ShouldFailWithInvalidStatus()
    {
        // Arrange: promote to Accepted
        var acceptedOrderId = await OrderLifecycleTestHelper.CreateAcceptedOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();

        // Act
        var result = await SendAsync(new RejectOrderCommand(acceptedOrderId.Value, Testing.TestData.DefaultRestaurantId, "Too late"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(OrderErrors.InvalidStatusForReject.Code);

        var order = await FindOrderAsync(acceptedOrderId);
        order!.Status.Should().Be(OrderStatus.Accepted); // unchanged
    }

    [Test]
    public async Task Reject_AsCustomer_ShouldFail()
    {
        // Arrange (default context is customer)
        var orderId = await OrderLifecycleTestHelper.CreatePlacedOrderAsync();

        // Act
        var act = async () => await SendAsync(new RejectOrderCommand(orderId.Value, Testing.TestData.DefaultRestaurantId, "No reason"));

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
        var order = await FindOrderAsync(orderId);
        order!.Status.Should().Be(OrderStatus.Placed);
    }
}
