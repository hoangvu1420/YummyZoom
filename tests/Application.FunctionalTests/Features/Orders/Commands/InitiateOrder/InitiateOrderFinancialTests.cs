using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Orders.Commands.InitiateOrder;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;

/// <summary>
/// Tests for InitiateOrder command financial calculations.
/// Focuses on accurate financial calculations, proper money handling, and coupon discount logic validation.
/// Uses real menu item prices from test data and verifies calculations match expected business rules.
/// </summary>
public class InitiateOrderFinancialTests : InitiateOrderTestBase
{
    #region Subtotal Calculation Tests

    [Test]
    public async Task InitiateOrder_WithSingleItem_ShouldCalculateSubtotalCorrectly()
    {
        // Arrange
        var classicBurgerItems = new List<OrderItemDto>
        {
            new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1)
        };

        var command = InitiateOrderTestHelper.BuildValidCommand(
            menuItemIds: new List<Guid> { classicBurgerItems[0].MenuItemId });

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        // Verify order was persisted with correct subtotal
        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();

        // Classic Burger price: 383,760 VND * 1 = 383,760 VND
        const decimal expectedSubtotal = 383760m;
        orderInDb!.Subtotal.Amount.Should().Be(expectedSubtotal);
        orderInDb.Subtotal.Currency.Should().Be(InitiateOrderTestHelper.TestAmounts.Currency);

        // Verify individual order item calculation
        orderInDb.OrderItems.Should().HaveCount(1);
        var orderItem = orderInDb.OrderItems.First();
        orderItem.LineItemTotal.Amount.Should().Be(expectedSubtotal);
        orderItem.Quantity.Should().Be(1);
    }

    [Test]
    public async Task InitiateOrder_WithMultipleItems_ShouldCalculateSubtotalCorrectly()
    {
        // Arrange
        var menuItemIds = Testing.TestData.GetMenuItemIds(
            Testing.TestData.MenuItems.ClassicBurger,   // $15.99
            Testing.TestData.MenuItems.BuffaloWings     // $12.99
        );

        var items = new List<OrderItemDto>
        {
            new(menuItemIds[0], 2),  // Classic Burger x2 = $31.98
            new(menuItemIds[1], 1)   // Buffalo Wings x1 = $12.99
        };

        var command = new InitiateOrderCommand(
            CustomerId: Testing.TestData.DefaultCustomerId,
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: items,
            DeliveryAddress: InitiateOrderTestHelper.DefaultDeliveryAddress,
            PaymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard,
            SpecialInstructions: null,
            CouponCode: null,
            TipAmount: null,
            TeamCartId: null
        );

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        // Verify order was persisted with correct subtotal
        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();

        // Total: (383,760 * 2) + (311,760 * 1) = 767,520 + 311,760 = 1,079,280 VND
        const decimal expectedSubtotal = 1079280m;
        orderInDb!.Subtotal.Amount.Should().Be(expectedSubtotal);

        // Verify individual line items are calculated correctly
        orderInDb.OrderItems.Should().HaveCount(2);
        var burgerItem = orderInDb.OrderItems.First(oi => oi.Quantity == 2);
        var wingsItem = orderInDb.OrderItems.First(oi => oi.Quantity == 1);

        burgerItem.LineItemTotal.Amount.Should().Be(767520m); // 383,760 * 2
        wingsItem.LineItemTotal.Amount.Should().Be(311760m); // 311,760 * 1
    }

    [Test]
    public async Task InitiateOrder_WithVariousQuantities_ShouldCalculateSubtotalCorrectly()
    {
        // Arrange
        var menuItemIds = Testing.TestData.GetMenuItemIds(
            Testing.TestData.MenuItems.MargheritaPizza,  // $18.50
            Testing.TestData.MenuItems.CaesarSalad,      // $9.99
            Testing.TestData.MenuItems.CraftBeer         // $6.99
        );

        var items = new List<OrderItemDto>
        {
            new(menuItemIds[0], 1),  // Margherita Pizza x1 = $18.50
            new(menuItemIds[1], 2),  // Caesar Salad x2 = $19.98
            new(menuItemIds[2], 3)   // Craft Beer x3 = $20.97
        };

        var command = new InitiateOrderCommand(
            CustomerId: Testing.TestData.DefaultCustomerId,
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: items,
            DeliveryAddress: InitiateOrderTestHelper.DefaultDeliveryAddress,
            PaymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard,
            SpecialInstructions: null,
            CouponCode: null,
            TipAmount: null,
            TeamCartId: null
        );

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        // Verify order was persisted with correct subtotal
        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();

        // Total: (444,000 * 1) + (239,760 * 2) + (167,760 * 3) = 444,000 + 479,520 + 503,280 = 1,426,800 VND
        const decimal expectedSubtotal = 1426800m;
        orderInDb!.Subtotal.Amount.Should().Be(expectedSubtotal);

        // Verify each line item calculation
        orderInDb.OrderItems.Should().HaveCount(3);
        foreach (var orderItem in orderInDb.OrderItems)
        {
            if (orderItem.Quantity == 1)
                orderItem.LineItemTotal.Amount.Should().Be(444000m); // Pizza: 444,000 * 1
            else if (orderItem.Quantity == 2)
                orderItem.LineItemTotal.Amount.Should().Be(479520m); // Salad: 239,760 * 2
            else if (orderItem.Quantity == 3)
                orderItem.LineItemTotal.Amount.Should().Be(503280m); // Beer: 167,760 * 3
        }
    }

    #endregion

    #region Tax and Fee Calculation Tests

    [Test]
    public async Task InitiateOrder_ShouldCalculateTaxAndDeliveryFeeCorrectly()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand();

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        // Verify order was persisted with correct tax and delivery fee
        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();

        // Verify delivery fee is applied correctly (should be 15000 VND as per StaticPricingService)
        orderInDb!.DeliveryFee.Amount.Should().Be(InitiateOrderTestHelper.TestAmounts.StandardDeliveryFee);
        orderInDb.DeliveryFee.Currency.Should().Be(InitiateOrderTestHelper.TestAmounts.Currency);

        // Verify tax amount is calculated at correct rate (8% of subtotal)
        var expectedTaxAmount = orderInDb.Subtotal.Amount * InitiateOrderTestHelper.TestAmounts.TaxRate;
        orderInDb.TaxAmount.Amount.Should().BeApproximately(expectedTaxAmount, 0.01m);
        orderInDb.TaxAmount.Currency.Should().Be(InitiateOrderTestHelper.TestAmounts.Currency);

        // Verify total includes all components
        var expectedTotal = orderInDb.Subtotal.Amount + orderInDb.DeliveryFee.Amount + orderInDb.TaxAmount.Amount;
        orderInDb.TotalAmount.Amount.Should().BeApproximately(expectedTotal, 0.01m);
    }

    [Test]
    public async Task InitiateOrder_WithLargeOrder_ShouldCalculateTaxCorrectly()
    {
        // Arrange - Create a large order to test tax calculation on higher amounts
        var menuItemIds = Testing.TestData.GetMenuItemIds(
            Testing.TestData.MenuItems.GrilledSalmon,    // $24.99
            Testing.TestData.MenuItems.MargheritaPizza,  // $18.50
            Testing.TestData.MenuItems.BuffaloWings      // $12.99
        );

        var items = new List<OrderItemDto>
        {
            new(menuItemIds[0], 2),  // Grilled Salmon x2 = $49.98
            new(menuItemIds[1], 1),  // Margherita Pizza x1 = $18.50
            new(menuItemIds[2], 2)   // Buffalo Wings x2 = $25.98
        };

        var command = new InitiateOrderCommand(
            CustomerId: Testing.TestData.DefaultCustomerId,
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: items,
            DeliveryAddress: InitiateOrderTestHelper.DefaultDeliveryAddress,
            PaymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard,
            SpecialInstructions: null,
            CouponCode: null,
            TipAmount: null,
            TeamCartId: null
        );

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();

        // Subtotal: 1,199,520 + 444,000 + 623,520 = 2,267,040 VND
        const decimal expectedSubtotal = 2267040m;
        orderInDb!.Subtotal.Amount.Should().Be(expectedSubtotal);

        // Tax: 2,267,040 * 8% = 181,363.2 ≈ 181,363 VND
        var expectedTaxAmount = expectedSubtotal * InitiateOrderTestHelper.TestAmounts.TaxRate;
        orderInDb.TaxAmount.Amount.Should().BeApproximately(expectedTaxAmount, 1m);

        // Total: 2,267,040 + 15,000 + 181,363 = 2,463,403 VND
        var expectedTotal = expectedSubtotal + InitiateOrderTestHelper.TestAmounts.StandardDeliveryFee + expectedTaxAmount;
        orderInDb.TotalAmount.Amount.Should().BeApproximately(expectedTotal, 1m);
    }

    #endregion

    #region Discount Calculation Tests

    [Test]
    public async Task InitiateOrder_WithPercentageCoupon_ShouldCalculateDiscountCorrectly()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommandWithCoupon(Testing.TestData.DefaultCouponCode);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        // Verify order was created with discount applied
        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();
        orderInDb!.AppliedCouponId.Should().NotBeNull();
        orderInDb.DiscountAmount.Amount.Should().BeGreaterThan(0);

        // Verify discount calculation (15% of subtotal as per DefaultTestData.Coupon.DiscountPercentage)
        var expectedDiscountAmount = orderInDb.Subtotal.Amount * 0.15m;
        orderInDb.DiscountAmount.Amount.Should().BeApproximately(expectedDiscountAmount, 0.01m);

        // Verify final total reflects discount
        var expectedTotal = orderInDb.Subtotal.Amount
                          - orderInDb.DiscountAmount.Amount
                          + orderInDb.DeliveryFee.Amount
                          + orderInDb.TaxAmount.Amount
                          + orderInDb.TipAmount.Amount;
        orderInDb.TotalAmount.Amount.Should().BeApproximately(expectedTotal, 0.01m);
    }

    [Test]
    public async Task InitiateOrder_WithCouponOnLargeOrder_ShouldCalculateDiscountCorrectly()
    {
        // Arrange - Large order to test percentage discount on higher amounts
        var menuItemIds = Testing.TestData.GetMenuItemIds(
            Testing.TestData.MenuItems.GrilledSalmon,    // $24.99
            Testing.TestData.MenuItems.MargheritaPizza   // $18.50
        );

        var items = new List<OrderItemDto>
        {
            new(menuItemIds[0], 3),  // Grilled Salmon x3 = $74.97
            new(menuItemIds[1], 2)   // Margherita Pizza x2 = $37.00
        };

        var command = new InitiateOrderCommand(
            CustomerId: Testing.TestData.DefaultCustomerId,
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: items,
            DeliveryAddress: InitiateOrderTestHelper.DefaultDeliveryAddress,
            PaymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard,
            SpecialInstructions: null,
            CouponCode: Testing.TestData.DefaultCouponCode,
            TipAmount: null,
            TeamCartId: null
        );

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();

        // Subtotal: 1,799,280 + 888,000 = 2,687,280 VND
        const decimal expectedSubtotal = 2687280m;
        orderInDb!.Subtotal.Amount.Should().Be(expectedSubtotal);

        // Discount: 2,687,280 * 15% = 403,092 VND
        var expectedDiscountAmount = expectedSubtotal * 0.15m;
        orderInDb.DiscountAmount.Amount.Should().BeApproximately(expectedDiscountAmount, 1m);

        // Verify coupon was applied
        orderInDb.AppliedCouponId.Should().NotBeNull();
    }

    #endregion

    #region Total Calculation Tests

    [Test]
    public async Task InitiateOrder_ShouldCalculateFinalTotalCorrectly()
    {
        // Arrange
        var tipAmount = InitiateOrderTestHelper.TestAmounts.MediumTip; // $5.00
        var command = InitiateOrderTestHelper.BuildValidCommandWithTip(tipAmount);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();

        // Verify tip was applied
        orderInDb!.TipAmount.Amount.Should().Be(tipAmount);

        // Calculate expected total: subtotal + delivery fee + tip + tax
        var expectedTotal = orderInDb.Subtotal.Amount
                          + orderInDb.DeliveryFee.Amount
                          + orderInDb.TipAmount.Amount
                          + orderInDb.TaxAmount.Amount;

        orderInDb.TotalAmount.Amount.Should().BeApproximately(expectedTotal, 0.01m);

        // Verify all components are properly included
        orderInDb.DeliveryFee.Amount.Should().Be(InitiateOrderTestHelper.TestAmounts.StandardDeliveryFee);
        orderInDb.TaxAmount.Amount.Should().BeGreaterThan(0);
        orderInDb.DiscountAmount.Amount.Should().Be(0); // No coupon applied
    }

    [Test]
    public async Task InitiateOrder_WithAllComponents_ShouldCalculateFinalTotalCorrectly()
    {
        // Arrange - Order with subtotal, delivery fee, tip, tax, and discount
        var tipAmount = InitiateOrderTestHelper.TestAmounts.LargeTip; // $10.00
        var command = new InitiateOrderCommand(
            CustomerId: Testing.TestData.DefaultCustomerId,
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<OrderItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.GrilledSalmon), 2) // $24.99 x 2 = $49.98
            },
            DeliveryAddress: InitiateOrderTestHelper.DefaultDeliveryAddress,
            PaymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard,
            SpecialInstructions: "Test order with all components",
            CouponCode: Testing.TestData.DefaultCouponCode,
            TipAmount: tipAmount,
            TeamCartId: null
        );

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();

        // Verify all financial components
        const decimal expectedSubtotal = 1199520m; // 599,760 * 2 VND
        orderInDb!.Subtotal.Amount.Should().Be(expectedSubtotal);

        var expectedDiscountAmount = expectedSubtotal * 0.15m; // 15% coupon
        orderInDb.DiscountAmount.Amount.Should().BeApproximately(expectedDiscountAmount, 1m);

        orderInDb.DeliveryFee.Amount.Should().Be(InitiateOrderTestHelper.TestAmounts.StandardDeliveryFee);
        orderInDb.TipAmount.Amount.Should().Be(tipAmount);

        var expectedTaxAmount = expectedSubtotal * InitiateOrderTestHelper.TestAmounts.TaxRate;
        orderInDb.TaxAmount.Amount.Should().BeApproximately(expectedTaxAmount, 0.01m);

        // Calculate expected total: subtotal - discount + delivery fee + tip + tax
        var expectedTotal = expectedSubtotal
                          - expectedDiscountAmount
                          + InitiateOrderTestHelper.TestAmounts.StandardDeliveryFee
                          + tipAmount
                          + expectedTaxAmount;

        orderInDb.TotalAmount.Amount.Should().BeApproximately(expectedTotal, 0.01m);
        response.TotalAmount.Amount.Should().BeApproximately(expectedTotal, 0.01m);
    }

    [Test]
    public async Task InitiateOrder_WithZeroTip_ShouldCalculateTotalCorrectly()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand(); // No tip specified

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();

        // Verify no tip was applied
        orderInDb!.TipAmount.Amount.Should().Be(0);

        // Calculate expected total without tip: subtotal + delivery fee + tax
        var expectedTotal = orderInDb.Subtotal.Amount
                          + orderInDb.DeliveryFee.Amount
                          + orderInDb.TaxAmount.Amount;

        orderInDb.TotalAmount.Amount.Should().BeApproximately(expectedTotal, 0.01m);
    }

    #endregion

    #region Edge Cases and Precision Tests

    [Test]
    public async Task InitiateOrder_WithDecimalPrices_ShouldMaintainPrecision()
    {
        // Arrange - Items with decimal prices to test precision handling
        var menuItemIds = Testing.TestData.GetMenuItemIds(
            Testing.TestData.MenuItems.CaesarSalad,  // $9.99
            Testing.TestData.MenuItems.FreshJuice    // $4.99
        );

        var items = new List<OrderItemDto>
        {
            new(menuItemIds[0], 3),  // Caesar Salad x3 = $29.97
            new(menuItemIds[1], 2)   // Fresh Juice x2 = $9.98
        };

        var command = new InitiateOrderCommand(
            CustomerId: Testing.TestData.DefaultCustomerId,
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: items,
            DeliveryAddress: InitiateOrderTestHelper.DefaultDeliveryAddress,
            PaymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard,
            SpecialInstructions: null,
            CouponCode: null,
            TipAmount: 2.33m, // Non-round tip amount
            TeamCartId: null
        );

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();

        // Verify precise subtotal: 719,280 + 239,520 = 958,800 VND
        const decimal expectedSubtotal = 958800m;
        orderInDb!.Subtotal.Amount.Should().Be(expectedSubtotal);

        // Verify tip precision
        orderInDb.TipAmount.Amount.Should().Be(2.33m);

        // Verify tax calculation maintains precision
        var expectedTaxAmount = expectedSubtotal * InitiateOrderTestHelper.TestAmounts.TaxRate;
        orderInDb.TaxAmount.Amount.Should().BeApproximately(expectedTaxAmount, 0.01m);

        // Verify final total precision
        var expectedTotal = expectedSubtotal
                          + InitiateOrderTestHelper.TestAmounts.StandardDeliveryFee
                          + 2.33m
                          + expectedTaxAmount;
        orderInDb.TotalAmount.Amount.Should().BeApproximately(expectedTotal, 0.01m);
    }

    [Test]
    public async Task InitiateOrder_WithMinimumOrder_ShouldCalculateCorrectly()
    {
        // Arrange - Single cheapest item to test minimum order calculations
        var command = new InitiateOrderCommand(
            CustomerId: Testing.TestData.DefaultCustomerId,
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<OrderItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.FreshJuice), 1) // $4.99
            },
            DeliveryAddress: InitiateOrderTestHelper.DefaultDeliveryAddress,
            PaymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard,
            SpecialInstructions: null,
            CouponCode: null,
            TipAmount: null,
            TeamCartId: null
        );

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();

        // Verify minimum order calculations
        const decimal expectedSubtotal = 119760m; // 119,760 VND
        orderInDb!.Subtotal.Amount.Should().Be(expectedSubtotal);

        // Tax should still be calculated correctly on small amounts
        var expectedTaxAmount = expectedSubtotal * InitiateOrderTestHelper.TestAmounts.TaxRate;
        orderInDb.TaxAmount.Amount.Should().BeApproximately(expectedTaxAmount, 1m);

        // Total should include all components even for small orders
        var expectedTotal = expectedSubtotal
                          + InitiateOrderTestHelper.TestAmounts.StandardDeliveryFee
                          + expectedTaxAmount;
        orderInDb.TotalAmount.Amount.Should().BeApproximately(expectedTotal, 0.01m);
    }

    #endregion
}
