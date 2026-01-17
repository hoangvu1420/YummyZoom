using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Orders.Queries.GetCustomerRecentOrders;
using YummyZoom.Application.Orders.Queries.GetOrderById;
using YummyZoom.Application.Orders.Queries.GetOrderStatus;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Queries;

[TestFixture]
public class OrderOriginAndPaymentBreakdownProjectionTests : BaseTestFixture
{
    [SetUp]
    public void SetUpUser()
    {
        SetUserId(Testing.TestData.DefaultCustomerId);
    }

    private static async Task<(Guid OrderId, Guid SourceTeamCartId)> SeedTeamCartOriginOrderWithMixedSucceededPaymentsAsync()
    {
        var currency = "USD";
        var timestamp = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var sourceTeamCartId = Guid.NewGuid();

        var customerId = UserId.Create(Testing.TestData.DefaultCustomerId);
        var restaurantId = RestaurantId.Create(Testing.TestData.DefaultRestaurantId);

        var address = DeliveryAddress.Create("123 Main St", "Test City", "TS", "12345", "US").Value;

        var orderItem = OrderItem.Create(
            MenuCategoryId.Create(Testing.TestData.GetMenuCategoryId(Testing.TestData.MenuCategories.MainDishes)),
            MenuItemId.Create(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger)),
            "Snapshot Burger",
            new Money(10m, currency),
            quantity: 1).Value;

        var subtotal = new Money(10m, currency);
        var discount = new Money(0m, currency);
        var deliveryFee = new Money(0m, currency);
        var tip = new Money(0m, currency);
        var tax = new Money(0m, currency);
        var total = new Money(10m, currency);

        var onlineTx = PaymentTransaction.Create(
            PaymentMethodType.CreditCard,
            PaymentTransactionType.Payment,
            new Money(7m, currency),
            timestamp.AddSeconds(1),
            paymentMethodDisplay: "CreditCard",
            paymentGatewayReferenceId: "pi_test_123",
            paidByUserId: customerId).Value;
        onlineTx.MarkAsSucceeded();

        var codTx = PaymentTransaction.Create(
            PaymentMethodType.CashOnDelivery,
            PaymentTransactionType.Payment,
            new Money(3m, currency),
            timestamp.AddSeconds(2),
            paymentMethodDisplay: "CashOnDelivery",
            paymentGatewayReferenceId: null,
            paidByUserId: customerId).Value;
        codTx.MarkAsSucceeded();

        var orderId = OrderId.CreateUnique();
        var order = Order.Create(
            orderId,
            customerId,
            restaurantId,
            address,
            new List<OrderItem> { orderItem },
            specialInstructions: string.Empty,
            subtotal,
            discount,
            deliveryFee,
            tip,
            tax,
            total,
            new List<PaymentTransaction> { onlineTx, codTx },
            appliedCouponId: null,
            initialStatus: OrderStatus.Placed,
            sourceTeamCartId: TeamCartId.Create(sourceTeamCartId),
            timestamp: timestamp).Value;

        await AddAsync(order);
        return (orderId.Value, sourceTeamCartId);
    }

    [Test]
    public async Task OrderOriginAndPaymentBreakdown_GetOrderById_ProjectsOriginAndSplitAmounts()
    {
        var (orderId, sourceTeamCartId) = await SeedTeamCartOriginOrderWithMixedSucceededPaymentsAsync();

        var result = await SendAsync(new GetOrderByIdQuery(orderId));

        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        result.Value.Order.SourceTeamCartId.Should().Be(sourceTeamCartId);
        result.Value.Order.IsFromTeamCart.Should().BeTrue();
        result.Value.Order.PaidOnlineAmount.Should().Be(7m);
        result.Value.Order.CashOnDeliveryAmount.Should().Be(3m);
    }

    [Test]
    public async Task OrderOriginAndPaymentBreakdown_GetCustomerRecentOrders_ProjectsOriginAndSplitAmounts()
    {
        var (orderId, sourceTeamCartId) = await SeedTeamCartOriginOrderWithMixedSucceededPaymentsAsync();

        var result = await SendAsync(new GetCustomerRecentOrdersQuery(PageNumber: 1, PageSize: 10));

        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        var item = result.Value.Items.Single(i => i.OrderId == orderId);
        item.SourceTeamCartId.Should().Be(sourceTeamCartId);
        item.IsFromTeamCart.Should().BeTrue();
        item.PaidOnlineAmount.Should().Be(7m);
        item.CashOnDeliveryAmount.Should().Be(3m);
    }

    [Test]
    public async Task OrderOriginAndPaymentBreakdown_GetOrderStatus_IncludesOriginFields()
    {
        var (orderId, sourceTeamCartId) = await SeedTeamCartOriginOrderWithMixedSucceededPaymentsAsync();

        var result = await SendAsync(new GetOrderStatusQuery(orderId));

        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        result.Value.SourceTeamCartId.Should().Be(sourceTeamCartId);
        result.Value.IsFromTeamCart.Should().BeTrue();
    }
}

