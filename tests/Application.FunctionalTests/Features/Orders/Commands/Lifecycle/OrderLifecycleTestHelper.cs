using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using YummyZoom.Application.FunctionalTests.UserManagement;
using YummyZoom.Application.Orders.Commands.AcceptOrder;
using YummyZoom.Application.Orders.Commands.CancelOrder;
using YummyZoom.Application.Orders.Commands.MarkOrderDelivered;
using YummyZoom.Application.Orders.Commands.MarkOrderPreparing;
using YummyZoom.Application.Orders.Commands.MarkOrderReadyForDelivery;
using YummyZoom.Application.Orders.Commands.RejectOrder;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;

/// <summary>
/// Helper utilities for order lifecycle tests: creation, promotion, command senders, context & assertions.
/// Uses lazy creation for default staff/owner users (Option A) to avoid global seeding complexity.
/// </summary>
public static class OrderLifecycleTestHelper
{
    private static Guid? _defaultStaffUserId;
    private static Guid? _defaultOwnerUserId;

    /// <summary>
    /// Creates a new placed order as the default customer via InitiateOrderTestHelper.
    /// </summary>
    public static async Task<OrderId> CreatePlacedOrderAsync()
    {
        SetUserId(Testing.TestData.DefaultCustomerId); // ensure customer context
        var initiateCommand = InitiateOrderTestHelper.BuildValidCommand(
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery);
        var result = await SendAsync(initiateCommand);
        result.IsSuccess.Should().BeTrue("InitiateOrder should succeed for valid data");
        var response = result.ValueOrFail();
        var orderId = response.OrderId; // InitiateOrderResponse.OrderId
        return orderId;
    }

    public static async Task<OrderId> CreateAcceptedOrderAsync(DateTime? etaUtc = null)
    {
        var orderId = await CreatePlacedOrderAsync();
        await RunAsDefaultRestaurantStaffAsync();
        await AcceptAsync(orderId, etaUtc ?? DateTime.UtcNow.AddMinutes(30));
        return orderId;
    }

    public static async Task<OrderId> CreatePreparingOrderAsync()
    {
        var orderId = await CreateAcceptedOrderAsync();
        await RunAsDefaultRestaurantStaffAsync();
        await MarkPreparingAsync(orderId);
        return orderId;
    }

    public static async Task<OrderId> CreateReadyOrderAsync()
    {
        var orderId = await CreatePreparingOrderAsync();
        await RunAsDefaultRestaurantStaffAsync();
        await MarkReadyAsync(orderId);
        return orderId;
    }

    #region Command Senders

    public static async Task<Result> AcceptAsync(OrderId orderId, DateTime estimatedDeliveryTimeUtc)
    {
        var cmd = new AcceptOrderCommand(orderId.Value, Testing.TestData.DefaultRestaurantId, estimatedDeliveryTimeUtc);
        var result = await SendAsync(cmd);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    public static async Task<Result> RejectAsync(OrderId orderId, string? reason = null)
    {
        var cmd = new RejectOrderCommand(orderId.Value, Testing.TestData.DefaultRestaurantId, reason);
        var result = await SendAsync(cmd);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    public static async Task<Result> CancelAsync(OrderId orderId, Guid? actingUserId, string? reason = null)
    {
        var cmd = new CancelOrderCommand(orderId.Value, Testing.TestData.DefaultRestaurantId, actingUserId, reason);
        var result = await SendAsync(cmd);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    public static async Task<Result> MarkPreparingAsync(OrderId orderId)
    {
        var cmd = new MarkOrderPreparingCommand(orderId.Value, Testing.TestData.DefaultRestaurantId);
        var result = await SendAsync(cmd);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    public static async Task<Result> MarkReadyAsync(OrderId orderId)
    {
        var cmd = new MarkOrderReadyForDeliveryCommand(orderId.Value, Testing.TestData.DefaultRestaurantId);
        var result = await SendAsync(cmd);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    public static async Task<Result> MarkDeliveredAsync(OrderId orderId, DateTime? deliveredAtUtc = null)
    {
        var cmd = new MarkOrderDeliveredCommand(orderId.Value, Testing.TestData.DefaultRestaurantId, deliveredAtUtc);
        var result = await SendAsync(cmd);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    #endregion

    #region Context Helpers

    public static async Task<Guid> RunAsDefaultRestaurantStaffAsync()
    {
        if (_defaultStaffUserId.HasValue)
        {
            SetUserId(_defaultStaffUserId.Value);

            // IMPORTANT: Re-add the permission claim when reusing the user
            TestAuthenticationService.AddPermissionClaim(Roles.RestaurantStaff,
                Testing.TestData.DefaultRestaurantId.ToString());

            return _defaultStaffUserId.Value;
        }

        _defaultStaffUserId =
            await RunAsRestaurantStaffAsync("default-staff@yummyzoom.test", Testing.TestData.DefaultRestaurantId);
        return _defaultStaffUserId.Value;
    }

    public static async Task<Guid> RunAsDefaultRestaurantOwnerAsync()
    {
        if (_defaultOwnerUserId.HasValue)
        {
            SetUserId(_defaultOwnerUserId.Value);

            // IMPORTANT: Re-add the permission claim when reusing the user
            TestAuthenticationService.AddPermissionClaim(Roles.RestaurantOwner,
                Testing.TestData.DefaultRestaurantId.ToString());

            return _defaultOwnerUserId.Value;
        }

        _defaultOwnerUserId =
            await RunAsRestaurantOwnerAsync("default-owner@yummyzoom.test", Testing.TestData.DefaultRestaurantId);
        return _defaultOwnerUserId.Value;
    }

    #endregion

    #region Assertions

    public static async Task<Order> AssertStatusAsync(OrderId orderId, string expectedStatus)
    {
        var order = await FindOrderAsync(orderId);
        order.Should().NotBeNull();
        order!.Status.ToString().Should().Be(expectedStatus);
        return order;
    }

    public static async Task AssertIdempotentAsync(OrderId orderId, Func<Task<Result>> action, string expectedStatus,
        Func<Order, DateTime?>? timestampSelector = null)
    {
        var firstOrder = await AssertStatusAsync(orderId, expectedStatus);
        var firstStamp = timestampSelector?.Invoke(firstOrder);

        var secondResult = await action();
        secondResult.IsSuccess.Should().BeTrue();

        var secondOrder = await AssertStatusAsync(orderId, expectedStatus);
        var secondStamp = timestampSelector?.Invoke(secondOrder);

        if (timestampSelector != null && firstStamp.HasValue && secondStamp.HasValue)
        {
            secondStamp.Should().Be(firstStamp, "Idempotent call should not alter timestamp");
        }
    }

    #endregion
}
