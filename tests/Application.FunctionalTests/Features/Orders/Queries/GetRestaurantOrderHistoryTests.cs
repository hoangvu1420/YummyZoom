using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;
using YummyZoom.Application.Orders.Queries.GetRestaurantOrderHistory;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Queries;

[TestFixture]
public class GetRestaurantOrderHistoryTests : BaseTestFixture
{
    private Guid _restaurantId;

    [SetUp]
    public async Task SetUpContext()
    {
        _restaurantId = Testing.TestData.DefaultRestaurantId;
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
    }

    private static GetRestaurantOrderHistoryQuery BuildQuery(
        Guid restaurantId,
        int page,
        int size,
        DateTime? from = null,
        DateTime? to = null,
        string? statuses = null,
        string? keyword = null)
        => new(restaurantId, page, size, from, to, statuses, keyword);

    private static async Task<OrderId> CreateDeliveredOrderAsync()
    {
        var orderId = await OrderLifecycleTestHelper.CreateReadyOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var result = await OrderLifecycleTestHelper.MarkDeliveredAsync(orderId);
        result.IsSuccess.Should().BeTrue();
        return orderId;
    }

    private static async Task<OrderId> CreateRejectedOrderAsync()
    {
        var orderId = await OrderLifecycleTestHelper.CreatePlacedOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var result = await OrderLifecycleTestHelper.RejectAsync(orderId);
        result.IsSuccess.Should().BeTrue();
        return orderId;
    }

    private static async Task<OrderId> CreateCancelledOrderAsync()
    {
        var orderId = await OrderLifecycleTestHelper.CreateAcceptedOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var result = await OrderLifecycleTestHelper.CancelAsync(orderId, null);
        result.IsSuccess.Should().BeTrue();
        return orderId;
    }

    [Test]
    public async Task HappyPath_ReturnsTerminalStatuses_RecentFirst()
    {
        await CreateDeliveredOrderAsync();
        await CreateRejectedOrderAsync();
        await CreateCancelledOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();

        var result = await SendAsync(BuildQuery(_restaurantId, 1, 10));
        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        result.Value.TotalCount.Should().Be(3);
        result.Value.Items.Should().NotBeEmpty();
        result.Value.Items.Select(i => i.Status).Distinct().Should().OnlyContain(s =>
            s == "Delivered" || s == "Cancelled" || s == "Rejected");
        var placements = result.Value.Items.Select(i => i.PlacementTimestamp).ToList();
        placements.Should().BeInDescendingOrder();
    }

    [Test]
    public async Task Filters_ByStatusAndKeyword()
    {
        var deliveredOrderId = await CreateDeliveredOrderAsync();
        await CreateCancelledOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();

        var deliveredOrder = await FindOrderAsync(deliveredOrderId);
        deliveredOrder.Should().NotBeNull();
        var keyword = deliveredOrder!.OrderNumber;

        var result = await SendAsync(BuildQuery(_restaurantId, 1, 10, statuses: "Delivered", keyword: keyword));
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Single().OrderNumber.Should().Be(keyword);
        result.Value.Items.Single().Status.Should().Be("Delivered");
    }

    [Test]
    public async Task Authorization_NonStaffForbidden()
    {
        await CreateDeliveredOrderAsync();

        await RunAsUserAsync("plain@yummyzoom.test", "Plain User", Array.Empty<string>());
        var act = async () => await SendAsync(BuildQuery(_restaurantId, 1, 10));
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Validation_InvalidPageSize_ShouldThrow()
    {
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var act = async () => await SendAsync(BuildQuery(_restaurantId, 1, 0));
        await act.Should().ThrowAsync<ValidationException>();
    }
}
