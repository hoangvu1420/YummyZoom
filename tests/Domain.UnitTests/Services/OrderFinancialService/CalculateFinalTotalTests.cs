using YummyZoom.Domain.Common.ValueObjects;

namespace YummyZoom.Domain.UnitTests.Services.OrderFinancialService;

/// <summary>
/// Tests for OrderFinancialService.CalculateFinalTotal method.
/// </summary>
public class CalculateFinalTotalTests : OrderFinancialServiceTestsBase
{
    #region Basic Calculation Tests

    [Test]
    public void CalculateFinalTotal_WithAllPositiveValues_ReturnsCorrectTotal()
    {
        // Arrange
        var subtotal = CreateMoney(100.00m);
        var discount = CreateMoney(10.00m);
        var deliveryFee = CreateMoney(5.00m);
        var tip = CreateMoney(15.00m);
        var tax = CreateMoney(8.50m);
        var expectedTotal = CreateMoney(118.50m); // 100 - 10 + 5 + 15 + 8.50

        // Act
        var result = _orderFinancialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should calculate correct total with all positive values");
    }

    [Test]
    public void CalculateFinalTotal_WithZeroValues_ReturnsSubtotal()
    {
        // Arrange
        var subtotal = CreateMoney(50.00m);
        var discount = ZeroMoney();
        var deliveryFee = ZeroMoney();
        var tip = ZeroMoney();
        var tax = ZeroMoney();
        var expectedTotal = CreateMoney(50.00m); // Only subtotal

        // Act
        var result = _orderFinancialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should return subtotal when all other values are zero");
    }

    [Test]
    public void CalculateFinalTotal_WithZeroSubtotal_ReturnsFeesAndTaxMinusDiscount()
    {
        // Arrange
        var subtotal = ZeroMoney();
        var discount = CreateMoney(2.00m);
        var deliveryFee = CreateMoney(5.00m);
        var tip = CreateMoney(3.00m);
        var tax = CreateMoney(1.00m);
        var expectedTotal = CreateMoney(7.00m); // 0 - 2 + 5 + 3 + 1

        // Act
        var result = _orderFinancialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should calculate correctly with zero subtotal");
    }

    #endregion

    #region Negative Total Prevention Tests

    [Test]
    public void CalculateFinalTotal_WithDiscountExceedingSubtotal_ReturnsZero()
    {
        // Arrange
        var subtotal = CreateMoney(20.00m);
        var discount = CreateMoney(25.00m); // Discount > subtotal
        var deliveryFee = ZeroMoney();
        var tip = ZeroMoney();
        var tax = ZeroMoney();
        var expectedTotal = ZeroMoney(); // Negative result becomes zero

        // Act
        var result = _orderFinancialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should return zero when discount exceeds subtotal");
    }

    [Test]
    public void CalculateFinalTotal_WithLargeDiscountAndSmallFees_ReturnsZero()
    {
        // Arrange
        var subtotal = CreateMoney(30.00m);
        var discount = CreateMoney(50.00m); // Large discount
        var deliveryFee = CreateMoney(2.00m);
        var tip = CreateMoney(1.00m);
        var tax = CreateMoney(1.50m);
        // Calculation: 30 - 50 + 2 + 1 + 1.5 = -15.5, should become 0
        var expectedTotal = ZeroMoney();

        // Act
        var result = _orderFinancialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should return zero when total calculation results in negative");
    }

    [Test]
    public void CalculateFinalTotal_WithExactlyZeroResult_ReturnsZero()
    {
        // Arrange
        var subtotal = CreateMoney(10.00m);
        var discount = CreateMoney(15.00m);
        var deliveryFee = CreateMoney(3.00m);
        var tip = CreateMoney(1.00m);
        var tax = CreateMoney(1.00m);
        // Calculation: 10 - 15 + 3 + 1 + 1 = 0
        var expectedTotal = ZeroMoney();

        // Act
        var result = _orderFinancialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should return zero when calculation results in exactly zero");
    }

    #endregion

    #region Decimal Precision Tests

    [Test]
    public void CalculateFinalTotal_WithDecimalValues_MaintainsPrecision()
    {
        // Arrange
        var subtotal = CreateMoney(19.99m);
        var discount = CreateMoney(1.50m);
        var deliveryFee = CreateMoney(2.75m);
        var tip = CreateMoney(3.25m);
        var tax = CreateMoney(1.85m);
        var expectedTotal = CreateMoney(26.34m); // 19.99 - 1.50 + 2.75 + 3.25 + 1.85

        // Act
        var result = _orderFinancialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should maintain decimal precision in calculations");
    }

    [Test]
    public void CalculateFinalTotal_WithVerySmallAmounts_HandlesCorrectly()
    {
        // Arrange
        var subtotal = CreateMoney(0.05m);
        var discount = CreateMoney(0.01m);
        var deliveryFee = CreateMoney(0.02m);
        var tip = CreateMoney(0.01m);
        var tax = CreateMoney(0.01m);
        var expectedTotal = CreateMoney(0.08m); // 0.05 - 0.01 + 0.02 + 0.01 + 0.01

        // Act
        var result = _orderFinancialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should handle very small amounts correctly");
    }

    [Test]
    public void CalculateFinalTotal_WithLargeAmounts_HandlesCorrectly()
    {
        // Arrange
        var subtotal = CreateMoney(9999.99m);
        var discount = CreateMoney(500.00m);
        var deliveryFee = CreateMoney(25.00m);
        var tip = CreateMoney(1500.00m);
        var tax = CreateMoney(850.50m);
        var expectedTotal = CreateMoney(11875.49m); // 9999.99 - 500 + 25 + 1500 + 850.50

        // Act
        var result = _orderFinancialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should handle large amounts correctly");
    }

    #endregion

    #region Currency Consistency Tests

    [Test]
    public void CalculateFinalTotal_WithSameCurrency_ReturnsSameCurrency()
    {
        // Arrange
        var subtotal = new Money(100.00m, "EUR");
        var discount = new Money(10.00m, "EUR");
        var deliveryFee = new Money(5.00m, "EUR");
        var tip = new Money(15.00m, "EUR");
        var tax = new Money(8.00m, "EUR");
        var expectedTotal = new Money(118.00m, "EUR");

        // Act
        var result = _orderFinancialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should maintain currency consistency");
    }

    [Test]
    public void CalculateFinalTotal_WithMixedCurrencies_UsesSubtotalCurrency()
    {
        // Arrange
        var subtotal = new Money(100.00m, "USD");
        var discount = new Money(10.00m, "EUR"); // Different currency
        var deliveryFee = new Money(5.00m, "GBP"); // Different currency
        var tip = new Money(15.00m, "CAD"); // Different currency
        var tax = new Money(8.00m, "JPY"); // Different currency
        
        // Act
        var result = _orderFinancialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

        // Assert
        result.Currency.Should().Be("USD", "should use subtotal currency for final result");
        result.Amount.Should().Be(118.00m, "should calculate amounts regardless of currency differences");
    }

    #endregion

    #region Edge Cases

    [Test]
    public void CalculateFinalTotal_WithAllZeroValues_ReturnsZero()
    {
        // Arrange
        var subtotal = ZeroMoney();
        var discount = ZeroMoney();
        var deliveryFee = ZeroMoney();
        var tip = ZeroMoney();
        var tax = ZeroMoney();
        var expectedTotal = ZeroMoney();

        // Act
        var result = _orderFinancialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should return zero when all values are zero");
    }

    [Test]
    public void CalculateFinalTotal_WithOnlyDiscount_ReturnsZeroWhenDiscountExceedsZeroSubtotal()
    {
        // Arrange
        var subtotal = ZeroMoney();
        var discount = CreateMoney(10.00m);
        var deliveryFee = ZeroMoney();
        var tip = ZeroMoney();
        var tax = ZeroMoney();
        var expectedTotal = ZeroMoney(); // 0 - 10 = -10, becomes 0

        // Act
        var result = _orderFinancialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should return zero when discount exceeds zero subtotal");
    }

    [Test]
    public void CalculateFinalTotal_WithOnlyFeesAndTax_ReturnsSum()
    {
        // Arrange
        var subtotal = ZeroMoney();
        var discount = ZeroMoney();
        var deliveryFee = CreateMoney(5.00m);
        var tip = CreateMoney(10.00m);
        var tax = CreateMoney(2.50m);
        var expectedTotal = CreateMoney(17.50m); // 0 - 0 + 5 + 10 + 2.5

        // Act
        var result = _orderFinancialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should return sum of fees and tax when subtotal and discount are zero");
    }

    [Test]
    public void CalculateFinalTotal_WithNearZeroResult_HandlesCorrectly()
    {
        // Arrange
        var subtotal = CreateMoney(10.01m);
        var discount = CreateMoney(10.00m);
        var deliveryFee = ZeroMoney();
        var tip = ZeroMoney();
        var tax = ZeroMoney();
        var expectedTotal = CreateMoney(0.01m); // 10.01 - 10.00

        // Act
        var result = _orderFinancialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should handle near-zero positive results correctly");
    }

    #endregion

    #region Real-World Scenarios

    [Test]
    public void CalculateFinalTotal_TypicalRestaurantOrder_CalculatesCorrectly()
    {
        // Arrange - Typical restaurant order scenario
        var subtotal = CreateMoney(45.75m);     // Food cost
        var discount = CreateMoney(4.58m);      // 10% coupon discount
        var deliveryFee = CreateMoney(3.99m);   // Delivery fee
        var tip = CreateMoney(8.00m);           // Tip
        var tax = CreateMoney(3.29m);           // Sales tax
        var expectedTotal = CreateMoney(56.45m); // 45.75 - 4.58 + 3.99 + 8.00 + 3.29

        // Act
        var result = _orderFinancialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should calculate typical restaurant order correctly");
    }

    [Test]
    public void CalculateFinalTotal_HighDiscountScenario_HandlesCorrectly()
    {
        // Arrange - High discount scenario (promotional offer)
        var subtotal = CreateMoney(25.00m);
        var discount = CreateMoney(20.00m);     // Large promotional discount
        var deliveryFee = CreateMoney(2.50m);
        var tip = CreateMoney(5.00m);
        var tax = CreateMoney(0.40m);           // Tax on reduced amount
        var expectedTotal = CreateMoney(12.90m); // 25 - 20 + 2.5 + 5 + 0.4

        // Act
        var result = _orderFinancialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

        // Assert
        AssertMoneyEquals(expectedTotal, result, "should handle high discount scenarios correctly");
    }

    #endregion
}