using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Entities;

namespace YummyZoom.Domain.UnitTests.Services.OrderFinancialService;

/// <summary>
/// Tests for OrderFinancialService.CalculateSubtotal method.
/// </summary>
public class CalculateSubtotalTests : OrderFinancialServiceTestsBase
{
    [Test]
    public void CalculateSubtotal_WithEmptyOrderItems_ReturnsZeroUSD()
    {
        // Arrange
        var orderItems = new List<OrderItem>();

        // Act
        var result = _orderFinancialService.CalculateSubtotal(orderItems);

        // Assert
        AssertMoneyEquals(Money.Zero("USD"), result, "empty order should return zero USD");
    }

    [Test]
    public void CalculateSubtotal_WithSingleItem_ReturnsItemTotal()
    {
        // Arrange
        var orderItem = CreateOrderItem(basePriceAmount: 15.50m, quantity: 2);
        var orderItems = new List<OrderItem> { orderItem };
        var expectedTotal = new Money(31.00m, "USD"); // 15.50 * 2

        // Act
        var result = _orderFinancialService.CalculateSubtotal(orderItems);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "single item subtotal should equal item total");
    }

    [Test]
    public void CalculateSubtotal_WithMultipleItems_ReturnsSumOfAllItems()
    {
        // Arrange
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(basePriceAmount: 10.00m, quantity: 1), // 10.00
            CreateOrderItem(basePriceAmount: 15.50m, quantity: 2), // 31.00
            CreateOrderItem(basePriceAmount: 8.75m, quantity: 3)   // 26.25
        };
        var expectedTotal = new Money(67.25m, "USD"); // 10.00 + 31.00 + 26.25

        // Act
        var result = _orderFinancialService.CalculateSubtotal(orderItems);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "multiple items subtotal should equal sum of all items");
    }

    [Test]
    public void CalculateSubtotal_WithDifferentCurrencies_UsesCurrencyFromFirstItem()
    {
        // Arrange
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(basePriceAmount: 10.00m, quantity: 1, currency: "EUR"),
            CreateOrderItem(basePriceAmount: 15.00m, quantity: 1, currency: "USD") // Different currency
        };
        var expectedTotal = new Money(25.00m, "EUR"); // Uses EUR from first item

        // Act
        var result = _orderFinancialService.CalculateSubtotal(orderItems);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should use currency from first item");
    }

    [Test]
    public void CalculateSubtotal_WithZeroPriceItems_IncludesZeroValueItems()
    {
        // Arrange
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(basePriceAmount: 0.00m, quantity: 1),   // Free item
            CreateOrderItem(basePriceAmount: 10.00m, quantity: 1)   // Paid item
        };
        var expectedTotal = new Money(10.00m, "USD");

        // Act
        var result = _orderFinancialService.CalculateSubtotal(orderItems);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should include zero-price items in calculation");
    }

    [Test]
    public void CalculateSubtotal_WithHighQuantities_HandlesLargeNumbers()
    {
        // Arrange
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(basePriceAmount: 5.99m, quantity: 100)
        };
        var expectedTotal = new Money(599.00m, "USD");

        // Act
        var result = _orderFinancialService.CalculateSubtotal(orderItems);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should handle high quantities correctly");
    }

    [Test]
    public void CalculateSubtotal_WithDecimalPrices_MaintainsPrecision()
    {
        // Arrange
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(basePriceAmount: 3.33m, quantity: 3), // 9.99
            CreateOrderItem(basePriceAmount: 1.11m, quantity: 1)  // 1.11
        };
        var expectedTotal = new Money(11.10m, "USD"); // 9.99 + 1.11

        // Act
        var result = _orderFinancialService.CalculateSubtotal(orderItems);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should maintain decimal precision");
    }

    [Test]
    public void CalculateSubtotal_WithSingleZeroPriceItem_ReturnsZero()
    {
        // Arrange
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(basePriceAmount: 0.00m, quantity: 5)
        };
        var expectedTotal = Money.Zero("USD");

        // Act
        var result = _orderFinancialService.CalculateSubtotal(orderItems);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "zero-price items should result in zero total");
    }

    [Test]
    public void CalculateSubtotal_WithVerySmallAmounts_HandlesMinimalPrecision()
    {
        // Arrange
        var orderItems = new List<OrderItem>
        {
            CreateOrderItem(basePriceAmount: 0.01m, quantity: 1),
            CreateOrderItem(basePriceAmount: 0.01m, quantity: 1)
        };
        var expectedTotal = new Money(0.02m, "USD");

        // Act
        var result = _orderFinancialService.CalculateSubtotal(orderItems);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should handle very small amounts correctly");
    }
}
