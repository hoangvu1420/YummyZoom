using FluentAssertions;
using static YummyZoom.Application.FunctionalTests.Testing;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;
using YummyZoom.Application.Orders.Queries.GetRestaurantActiveOrders;
using YummyZoom.Application.Orders.Commands.AcceptOrder;
using YummyZoom.Application.Orders.Commands.MarkOrderPreparing;
using YummyZoom.Application.Orders.Commands.MarkOrderReadyForDelivery;
using YummyZoom.Application.Orders.Commands.MarkOrderDelivered;
using YummyZoom.SharedKernel.Constants;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Queries;

[TestFixture]
public class GetRestaurantActiveOrdersTests : BaseTestFixture
{
    private Guid _restaurantId;

    [SetUp]
    public async Task SetUpContext()
    {
        _restaurantId = Testing.TestData.DefaultRestaurantId;
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
    }

    private static GetRestaurantActiveOrdersQuery BuildQuery(Guid restaurantId, int page, int size) => new(restaurantId, page, size);

    private async Task<Guid> CreatePlacedOrderAsync()
    {
        var orderId = (await OrderLifecycleTestHelper.CreatePlacedOrderAsync()).Value;
        return orderId;
    }

    [Test]
    public async Task HappyPath_MultipleStatuses_InPriorityOrder()
    {
        // Create one per status progression
        var placed = await OrderLifecycleTestHelper.CreatePlacedOrderAsync();
        var accepted = await OrderLifecycleTestHelper.CreateAcceptedOrderAsync();
        var preparing = await OrderLifecycleTestHelper.CreatePreparingOrderAsync();
        var ready = await OrderLifecycleTestHelper.CreateReadyOrderAsync();

        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var result = await SendAsync(BuildQuery(_restaurantId, 1, 20));
        result.IsSuccess.Should().BeTrue(result.Error?.ToString());

        var statuses = result.Value.Items.Select(i => i.Status).ToList();
        // Active statuses only
        statuses.All(s => new[]{"Placed","Accepted","Preparing","ReadyForDelivery"}.Contains(s)).Should().BeTrue();

        // Ensure priority ordering (first occurrence order)
        var firstIndex = (string status) => statuses.FindIndex(s => s == status);
        firstIndex("Placed").Should().BeLessThan(firstIndex("Accepted"));
        firstIndex("Accepted").Should().BeLessThan(firstIndex("Preparing"));
        firstIndex("Preparing").Should().BeLessThan(firstIndex("ReadyForDelivery"));
    }

    [Test]
    public async Task Paging_Works()
    {
        // 7 placed orders
        for (int i = 0; i < 7; i++) await CreatePlacedOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();

        var page1 = await SendAsync(BuildQuery(_restaurantId, 1, 5));
        var page2 = await SendAsync(BuildQuery(_restaurantId, 2, 5));

        page1.IsSuccess.Should().BeTrue();
        page2.IsSuccess.Should().BeTrue();
        page1.Value.Items.Count.Should().Be(5);
        page2.Value.Items.Count.Should().Be(2);
        page1.Value.TotalCount.Should().Be(7);
        page2.Value.TotalCount.Should().Be(7);
    }

    [Test]
    public async Task Excludes_NonActiveStatuses()
    {
        var placed = await OrderLifecycleTestHelper.CreatePlacedOrderAsync();
        var accepted = await OrderLifecycleTestHelper.CreateAcceptedOrderAsync();
        var preparing = await OrderLifecycleTestHelper.CreatePreparingOrderAsync();
        var ready = await OrderLifecycleTestHelper.CreateReadyOrderAsync();

        // Move one to Delivered (no longer active)
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        await SendAsync(new MarkOrderDeliveredCommand(ready.Value, _restaurantId, DateTime.UtcNow));

        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var result = await SendAsync(BuildQuery(_restaurantId, 1, 50));
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(i => i.Status).Should().NotContain("Delivered");
    }

    [Test]
    public async Task EmptyResult_NoActiveOrders()
    {
        // Create then push through to Delivered
        var order = await OrderLifecycleTestHelper.CreateReadyOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        await SendAsync(new MarkOrderDeliveredCommand(order.Value, _restaurantId, DateTime.UtcNow));

        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var result = await SendAsync(BuildQuery(_restaurantId, 1, 10));
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    [Test]
    public async Task Authorization_NonStaffForbidden()
    {
        await CreatePlacedOrderAsync();

        // Switch to plain user (no staff claims)
        await RunAsUserAsync("plain2@yummyzoom.test", "Plain User Two", Array.Empty<string>());
        var act = async () => await SendAsync(BuildQuery(_restaurantId, 1, 10));
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task PageBeyondRange_ReturnsEmpty()
    {
        for (int i = 0; i < 3; i++) await CreatePlacedOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var result = await SendAsync(BuildQuery(_restaurantId, 5, 5));
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(3);
    }

    [Test]
    public async Task Validation_InvalidPageSize_ShouldThrow()
    {
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var act = async () => await SendAsync(BuildQuery(_restaurantId, 1, 0));
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Validation_EmptyRestaurantId_ShouldThrow()
    {
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var act = async () => await SendAsync(BuildQuery(Guid.Empty, 1, 10));
        await act.Should().ThrowAsync<ValidationException>();
    }
}
