using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Orders.Commands.CancelOrder;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.SharedKernel.Constants;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;

/// <summary>
/// CancelOrder command tests covering authorization matrix, status restrictions, and idempotency.
/// Scenarios (per lifecycle plan):
/// 1. Customer cancels from Placed (allowed)
/// 2. Customer cancels from Preparing (should fail with InvalidStatusForCustomer error)
/// 3. Staff cancels from Preparing (allowed)
/// 4. Admin cancels from Accepted (allowed)
/// 5. Idempotent cancel (second cancel returns success & unchanged status)
/// 6. Unauthenticated cancel throws UnauthorizedAccessException
/// </summary>
public class CancelOrderTests : OrderLifecycleTestBase
{
    private static Guid DefaultRestaurantId => Testing.TestData.DefaultRestaurantId;

    private static CancelOrderCommand Build(Guid orderId, Guid? actingUserId, string? reason = null) =>
        new(orderId, DefaultRestaurantId, actingUserId, reason);

    [Test]
    public async Task Cancel_AsCustomer_FromPlaced_ShouldSucceed()
    {
        // Arrange (customer context is default)
        var orderId = await OrderLifecycleTestHelper.CreatePlacedOrderAsync();
        SetUserId(Testing.TestData.DefaultCustomerId); // ensure customer context

        // Act
        var result = await SendAsync(Build(orderId.Value, Testing.TestData.DefaultCustomerId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var order = await FindOrderAsync(orderId);
        order!.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Test]
    public async Task Cancel_AsCustomer_FromPreparing_ShouldFailWithInvalidStatusForCustomer()
    {
        // Arrange - move order to Preparing via staff actions, but use original customer when issuing cancel
        var orderId = await OrderLifecycleTestHelper.CreatePreparingOrderAsync();
        SetUserId(Testing.TestData.DefaultCustomerId); // switch back to customer

        // Act
        var result = await SendAsync(Build(orderId.Value, Testing.TestData.DefaultCustomerId));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CancelOrder.InvalidStatusForCustomer");
        var order = await FindOrderAsync(orderId);
        order!.Status.Should().Be(OrderStatus.Preparing);
    }

    [Test]
    public async Task Cancel_AsStaff_FromPreparing_ShouldSucceed()
    {
        // Arrange
        var orderId = await OrderLifecycleTestHelper.CreatePreparingOrderAsync();
        var staffUserId = await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();

        // Act (explicit acting user not strictly required but provided for coverage)
        var result = await SendAsync(Build(orderId.Value, staffUserId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var order = await FindOrderAsync(orderId);
        order!.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Test]
    public async Task Cancel_AsAdmin_FromAccepted_ShouldSucceed()
    {
        // Arrange - create accepted order
        var orderId = await OrderLifecycleTestHelper.CreateAcceptedOrderAsync();
        var adminUserId = await RunAsAdministratorAsync();

        // Act
        var result = await SendAsync(Build(orderId.Value, adminUserId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var order = await FindOrderAsync(orderId);
        order!.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Test]
    public async Task Cancel_WhenAlreadyCancelled_ShouldBeIdempotent()
    {
        // Arrange: cancel once as staff, then attempt again
        var orderId = await OrderLifecycleTestHelper.CreatePlacedOrderAsync();
        var staffUserId = await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var first = await SendAsync(Build(orderId.Value, staffUserId));
        first.IsSuccess.Should().BeTrue();
        var firstOrder = await FindOrderAsync(orderId);
        firstOrder!.Status.Should().Be(OrderStatus.Cancelled);
        var firstTimestamp = firstOrder.LastUpdateTimestamp;

        // Act second cancel (idempotent path inside handler will early-return success)
        var second = await SendAsync(Build(orderId.Value, staffUserId));

        // Assert
        second.IsSuccess.Should().BeTrue();
        var afterOrder = await FindOrderAsync(orderId);
        afterOrder!.Status.Should().Be(OrderStatus.Cancelled);
        afterOrder.LastUpdateTimestamp.Should().Be(firstTimestamp); // expect stable timestamp
    }

    [Test]
    public async Task Cancel_Unauthenticated_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var orderId = await OrderLifecycleTestHelper.CreatePlacedOrderAsync();
        ClearUserId(); // remove authentication

        // Act
        var act = async () => await SendAsync(Build(orderId.Value, null));

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        var order = await FindOrderAsync(orderId);
        order!.Status.Should().Be(OrderStatus.Placed);
    }
}
