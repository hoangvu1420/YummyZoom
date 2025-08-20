using FluentAssertions;
using static YummyZoom.Application.FunctionalTests.Testing;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Orders.Queries.GetOrderStatus;
using YummyZoom.Application.Orders.Commands.AcceptOrder;
using YummyZoom.Application.Orders.Commands.MarkOrderPreparing;
using YummyZoom.Application.Orders.Commands.MarkOrderReadyForDelivery;
using YummyZoom.Application.Orders.Commands.MarkOrderDelivered;
using YummyZoom.Application.Orders.Commands.CancelOrder;
using YummyZoom.Application.Common.Exceptions;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Queries;

[TestFixture]
public class GetOrderStatusTests : BaseTestFixture
{
    private Guid _restaurantId;

    [SetUp]
    public void SetupDefaults()
    {
        _restaurantId = Testing.TestData.DefaultRestaurantId;
        SetUserId(Testing.TestData.DefaultCustomerId);
    }

    private async Task<Guid> CreatePlacedOrderAsync()
    {
        var orderId = (await OrderLifecycleTestHelper.CreatePlacedOrderAsync()).Value;
        return orderId;
    }

    private static GetOrderStatusQuery BuildQuery(Guid orderId) => new(orderId);

    [Test]
    public async Task Customer_HappyPath_ReturnsStatus()
    {
        var orderId = await CreatePlacedOrderAsync();
        var result = await SendAsync(BuildQuery(orderId));
        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        result.Value.OrderId.Should().Be(orderId);
        result.Value.Status.Should().Be("Placed");
    }

    [Test]
    public async Task RestaurantStaff_HappyPath_ReturnsStatus()
    {
        var orderId = await CreatePlacedOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var result = await SendAsync(BuildQuery(orderId));
        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        result.Value.Status.Should().Be("Placed");
    }

    [Test]
    public async Task UnauthorizedOtherCustomer_ReturnsNotFound()
    {
        var orderId = await CreatePlacedOrderAsync();
        await RunAsUserAsync("other@yummyzoom.test", "Other User", Array.Empty<string>());
        var result = await SendAsync(BuildQuery(orderId));
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(GetOrderStatusErrors.NotFound);
    }

    [Test]
    public async Task NonexistentOrder_ReturnsNotFound()
    {
        var result = await SendAsync(BuildQuery(Guid.NewGuid()));
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(GetOrderStatusErrors.NotFound);
    }

    [Test]
    public async Task StatusProgression_ReflectsLatest()
    {
        var orderId = await CreatePlacedOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();

        var accept = await SendAsync(new AcceptOrderCommand(orderId, _restaurantId, DateTime.UtcNow.AddMinutes(30)));
        accept.IsSuccess.Should().BeTrue(accept.Error?.ToString());
        var afterAccept = await SendAsync(BuildQuery(orderId));
        afterAccept.Value.Status.Should().Be("Accepted");

        var preparing = await SendAsync(new MarkOrderPreparingCommand(orderId, _restaurantId));
        preparing.IsSuccess.Should().BeTrue(preparing.Error?.ToString());
        var afterPreparing = await SendAsync(BuildQuery(orderId));
        afterPreparing.Value.Status.Should().Be("Preparing");

        var ready = await SendAsync(new MarkOrderReadyForDeliveryCommand(orderId, _restaurantId));
        ready.IsSuccess.Should().BeTrue(ready.Error?.ToString());
        var afterReady = await SendAsync(BuildQuery(orderId));
        afterReady.Value.Status.Should().Be("ReadyForDelivery");

        var delivered = await SendAsync(new MarkOrderDeliveredCommand(orderId, _restaurantId, DateTime.UtcNow));
        delivered.IsSuccess.Should().BeTrue(delivered.Error?.ToString());
        var afterDelivered = await SendAsync(BuildQuery(orderId));
        afterDelivered.Value.Status.Should().Be("Delivered");

        afterDelivered.Value.LastUpdateTimestamp.Should()
            .BeOnOrAfter(afterAccept.Value.LastUpdateTimestamp);
    }

    [Test]
    public async Task CancelledOrder_ReturnsCancelledStatus()
    {
        var orderId = await CreatePlacedOrderAsync();
        var cancel = await SendAsync(new CancelOrderCommand(orderId, _restaurantId, Testing.TestData.DefaultCustomerId, "Change of mind"));
        cancel.IsSuccess.Should().BeTrue(cancel.Error?.ToString());
        var status = await SendAsync(BuildQuery(orderId));
        status.IsSuccess.Should().BeTrue(status.Error?.ToString());
        status.Value.Status.Should().Be("Cancelled");
    }

    [Test]
    public async Task Validation_EmptyGuid_ShouldThrow()
    {
        var act = async () => await SendAsync(BuildQuery(Guid.Empty));
        await act.Should().ThrowAsync<ValidationException>();
    }
}
