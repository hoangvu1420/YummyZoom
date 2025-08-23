using static YummyZoom.Application.FunctionalTests.Testing;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;
using YummyZoom.Application.Orders.Queries.GetRestaurantNewOrders;
using YummyZoom.Application.Orders.Commands.AcceptOrder;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Queries;

[TestFixture]
public class GetRestaurantNewOrdersTests : BaseTestFixture
{
    private Guid _restaurantId;

    [SetUp]
    public async Task SetUpContext()
    {
        // Create an order first to capture default restaurant id from test data indirectly
        _restaurantId = Testing.TestData.DefaultRestaurantId; // assuming exposed in test data
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
    }

    private async Task<Guid> CreatePlacedOrderAsync()
    {
        SetUserId(Testing.TestData.DefaultCustomerId); // ensure customer context
        var initiateCommand = InitiateOrderTestHelper.BuildValidCommand(
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery);
        var result = await SendAsync(initiateCommand);
        result.IsSuccess.Should().BeTrue("InitiateOrder should succeed for valid data");
        var response = result.ValueOrFail();
        var orderId = response.OrderId; // InitiateOrderResponse.OrderId
        return orderId.Value;
    }

    private async Task<List<Guid>> CreatePlacedOrdersAsync(int count)
    {
        var list = new List<Guid>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(await CreatePlacedOrderAsync());
        }
        return list;
    }

    private static GetRestaurantNewOrdersQuery BuildQuery(Guid restaurantId, int page, int size) => new(restaurantId, page, size);

    [Test]
    public async Task HappyPath_FirstPage_OldestFirst()
    {
        await CreatePlacedOrdersAsync(6);
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();

        var result = await SendAsync(BuildQuery(_restaurantId, 1, 5));
        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        result.Value.Items.Count.Should().Be(5);
        result.Value.TotalCount.Should().Be(6);
        result.Value.Items.Select(i => i.Status).Distinct().Should().Equal("Placed");
        var placements = result.Value.Items.Select(i => i.PlacementTimestamp).ToList();
        placements.Should().BeInAscendingOrder();
    }

    [Test]
    public async Task SecondPage_ReturnsRemaining()
    {
        await CreatePlacedOrdersAsync(6);
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();

        var page1 = await SendAsync(BuildQuery(_restaurantId, 1, 5));
        var page2 = await SendAsync(BuildQuery(_restaurantId, 2, 5));

        page1.IsSuccess.Should().BeTrue();
        page2.IsSuccess.Should().BeTrue();
        page2.Value.Items.Count.Should().Be(1);
        page2.Value.TotalCount.Should().Be(6);
        page2.Value.HasNextPage.Should().BeFalse();
    }

    [Test]
    public async Task FiltersOutAcceptedOrders()
    {
        var orderIds = await CreatePlacedOrdersAsync(3);
        var toAccept = orderIds.Take(1).Single();

        // Accept one order (needs staff context)
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var acceptResult = await SendAsync(new AcceptOrderCommand(toAccept, _restaurantId, DateTime.UtcNow.AddMinutes(30)));
        acceptResult.IsSuccess.Should().BeTrue();

        var queryResult = await SendAsync(BuildQuery(_restaurantId, 1, 10));
        queryResult.IsSuccess.Should().BeTrue();
        queryResult.Value.TotalCount.Should().Be(2); // only remaining placed
        queryResult.Value.Items.All(i => i.Status == "Placed").Should().BeTrue();
    }

    [Test]
    public async Task EmptyResult_NoPlacedOrders()
    {
        // Arrange: create a placed (cash-on-delivery) order then accept it so none remain in Placed
        var orderId = await CreatePlacedOrderAsync();
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        var acceptResult = await SendAsync(new AcceptOrderCommand(orderId, _restaurantId, DateTime.UtcNow.AddMinutes(25)));
        acceptResult.IsSuccess.Should().BeTrue();

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
        await RunAsUserAsync("plain@yummyzoom.test", "Plain User", Array.Empty<string>());
        var act = async () => await SendAsync(BuildQuery(_restaurantId, 1, 10));
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task PageBeyondRange_ReturnsEmpty()
    {
        await CreatePlacedOrdersAsync(3);
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
