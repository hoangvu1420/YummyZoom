using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.Orders.Commands.InitiateOrder;
using YummyZoom.Application.Orders.Queries.GetOrderById;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Queries;

[TestFixture]
public class GetOrderByIdTests : BaseTestFixture
{
    private Mock<IPaymentGatewayService> _paymentGatewayMock = null!;

    [SetUp]
    public async Task SetUpUserAndMocks()
    {
        // Authenticate as default customer (needed for InitiateOrderCommand authorization)
        SetUserId(Testing.TestData.DefaultCustomerId);

        // Mock payment gateway to avoid external calls during order creation
        _paymentGatewayMock = InitiateOrderTestHelper.SetupSuccessfulPaymentGatewayMock();
        ReplaceService<IPaymentGatewayService>(_paymentGatewayMock.Object);

        await Task.CompletedTask;
    }
    // Convenience accessors
    private Guid ClassicBurgerId => Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);

    private OrderItemDto BuildBurgerWithAddOns(params Guid[] choiceIds)
    { 
        var customization = new OrderItemCustomizationRequestDto(
            TestDataFactory.CustomizationGroup_BurgerAddOnsId,
            choiceIds.ToList());
        return new OrderItemDto(ClassicBurgerId, 1, new List<OrderItemCustomizationRequestDto> { customization });
    }

    private static GetOrderByIdQuery BuildQuery(Guid orderId) => new(orderId);

    [Test]
    public async Task Customer_HappyPath_ReturnsOrderDetails()
    {
        // Arrange (default customer context established by test infrastructure on first SendAsync)
        var initiate = InitiateOrderTestHelper.BuildValidCommand();
        var createResult = await SendAsync(initiate);
        createResult.IsSuccess.Should().BeTrue(createResult.Error?.ToString());

        // Act
        var queryResult = await SendAsync(BuildQuery(createResult.Value.OrderId.Value));

        // Assert
        queryResult.IsSuccess.Should().BeTrue(queryResult.Error?.ToString());
        var dto = queryResult.Value.Order;
        dto.OrderId.Should().Be(createResult.Value.OrderId.Value);
        dto.Items.Should().NotBeNull();
        dto.Items.Count.Should().Be(initiate.Items.Count);
        dto.Status.Should().NotBeNullOrWhiteSpace();
        dto.Currency.Should().NotBeNullOrWhiteSpace(); // Assert currency is present
        dto.TotalAmount.Should().BeGreaterThan(0m);
        dto.SubtotalAmount.Should().BeGreaterThan(0m);
        dto.DeliveryAddress_Street.Should().NotBeNull();
        dto.AppliedCouponId.Should().BeNull();
    }

    [Test]
    public async Task RestaurantStaff_HappyPath_ReturnsOrderDetails()
    {
        // Arrange create order as default customer
        var initiate = InitiateOrderTestHelper.BuildValidCommand();
        var createResult = await SendAsync(initiate);
        createResult.IsSuccess.Should().BeTrue(createResult.Error?.ToString());

        // Switch to restaurant staff context
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();

        // Act
        var queryResult = await SendAsync(BuildQuery(createResult.Value.OrderId.Value));

        // Assert
        queryResult.IsSuccess.Should().BeTrue(queryResult.Error?.ToString());
        queryResult.Value.Order.OrderId.Should().Be(createResult.Value.OrderId.Value);
    }

    [Test]
    public async Task UnauthorizedOtherCustomer_ShouldReturnNotFound()
    {
        // Arrange create order as default customer
        var initiate = InitiateOrderTestHelper.BuildValidCommand();
        var createResult = await SendAsync(initiate);
        createResult.IsSuccess.Should().BeTrue(createResult.Error?.ToString());

        // Run as a totally different plain user (no staff role)
        await RunAsUserAsync("othercustomer@yummyzoom.test", "Other Customer", Array.Empty<string>());

        // Act
        var queryResult = await SendAsync(BuildQuery(createResult.Value.OrderId.Value));

        // Assert (NotFound masking for unauthorized)
        queryResult.IsFailure.Should().BeTrue();
        queryResult.Error.Should().Be(GetOrderByIdErrors.NotFound);
    }

    [Test]
    public async Task NonexistentOrder_ShouldReturnNotFound()
    {
        // Act
        var queryResult = await SendAsync(BuildQuery(Guid.NewGuid()));

        // Assert
        queryResult.IsFailure.Should().BeTrue();
        queryResult.Error.Should().Be(GetOrderByIdErrors.NotFound);
    }

    [Test]
    public async Task OrderWithCustomizations_ShouldProjectParsedCustomizations()
    {
        // Arrange build command with one customized item (Extra Cheese + Bacon)
        var item = BuildBurgerWithAddOns(
            TestDataFactory.CustomizationChoice_ExtraCheeseId,
            TestDataFactory.CustomizationChoice_BaconId);
        var baseCommand = InitiateOrderTestHelper.BuildValidCommand(menuItemIds: new List<Guid> { ClassicBurgerId });
        var command = baseCommand with { Items = new List<OrderItemDto> { item } };

        var createResult = await SendAsync(command);
        createResult.IsSuccess.Should().BeTrue(createResult.Error?.ToString());

        // Act
        var queryResult = await SendAsync(BuildQuery(createResult.Value.OrderId.Value));

        // Assert
        queryResult.IsSuccess.Should().BeTrue(queryResult.Error?.ToString());
        var orderDto = queryResult.Value.Order;
        orderDto.Items.Count.Should().Be(1);
        var line = orderDto.Items[0];
        line.Customizations.Should().HaveCount(2);
        var names = line.Customizations.Select(c => c.ChoiceName).ToList();
        names.Should().Contain(new[] { "Extra Cheese", "Bacon" });
        // Basic financial sanity: line item total >= unit price
        line.LineItemTotalAmount.Should().BeGreaterThanOrEqualTo(line.UnitPriceAmount);
    }

    [Test]
    public async Task OrderWithCouponAndTip_ShouldReturnFinancialBreakdown()
    {
        // Arrange
        const decimal tip = 3.25m;
        var command = InitiateOrderTestHelper.BuildValidCommandWithCoupon(Testing.TestData.DefaultCouponCode) with { TipAmount = tip };
        var createResult = await SendAsync(command);
        createResult.IsSuccess.Should().BeTrue(createResult.Error?.ToString());

        // Act
        var queryResult = await SendAsync(BuildQuery(createResult.Value.OrderId.Value));

        // Assert
        queryResult.IsSuccess.Should().BeTrue(queryResult.Error?.ToString());
        var order = queryResult.Value.Order;
        order.AppliedCouponId.Should().NotBeNull();
        order.TipAmount.Should().Be(tip);
        order.DiscountAmount.Should().BeGreaterThan(0m);
        order.TotalAmount.Should().BeGreaterThan(0m);
        order.SubtotalAmount.Should().BeGreaterThan(0m);
        order.Currency.Should().Be("USD"); // Assert top-level currency

        // Optional lightweight arithmetic check
        var reconstructed = order.SubtotalAmount - order.DiscountAmount + order.DeliveryFeeAmount + order.TaxAmount + order.TipAmount;
        reconstructed.Should().Be(order.TotalAmount);
    }
}
