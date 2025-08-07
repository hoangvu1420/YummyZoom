using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;

namespace YummyZoom.Domain.UnitTests.Common.ValueObjects;

[TestFixture]
public class MoneyTests
{
    private const decimal DefaultAmount = 10.50m;
    private const string DefaultCurrency = Currencies.USD;
    private const string AlternateCurrency = "EUR";

    #region Constructor Tests

    [Test]
    public void Constructor_WithValidInputs_ShouldCreateMoneyCorrectly()
    {
        // Arrange & Act
        var money = new Money(DefaultAmount, DefaultCurrency);

        // Assert
        money.Amount.Should().Be(DefaultAmount);
        money.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void Constructor_WithZeroAmount_ShouldCreateMoneyCorrectly()
    {
        // Arrange & Act
        var money = new Money(0m, DefaultCurrency);

        // Assert
        money.Amount.Should().Be(0m);
        money.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void Constructor_WithNegativeAmount_ShouldCreateMoneyCorrectly()
    {
        // Arrange
        var negativeAmount = -5.25m;

        // Act
        var money = new Money(negativeAmount, DefaultCurrency);

        // Assert
        money.Amount.Should().Be(negativeAmount);
        money.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void Constructor_WithNullCurrency_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new Money(DefaultAmount, null!));
        exception!.ParamName.Should().Be("currency");
        exception.Message.Should().Contain("Currency cannot be null or empty.");
    }

    [Test]
    public void Constructor_WithEmptyCurrency_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new Money(DefaultAmount, string.Empty));
        exception!.ParamName.Should().Be("currency");
        exception.Message.Should().Contain("Currency cannot be null or empty.");
    }

    [Test]
    public void Constructor_WithWhitespaceCurrency_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new Money(DefaultAmount, "   "));
        exception!.ParamName.Should().Be("currency");
        exception.Message.Should().Contain("Currency cannot be null or empty.");
    }

    #endregion

    #region Zero() Static Method Tests

    [Test]
    public void Zero_WithValidCurrency_ShouldCreateZeroMoney()
    {
        // Arrange & Act
        var money = Money.Zero(DefaultCurrency);

        // Assert
        money.Amount.Should().Be(0m);
        money.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void Zero_WithNullCurrency_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => Money.Zero(null!));
        exception!.ParamName.Should().Be("currency");
    }

    #endregion

    #region Addition Operator Tests

    [Test]
    public void AdditionOperator_WithSameCurrency_ShouldAddAmountsCorrectly()
    {
        // Arrange
        var money1 = new Money(10.50m, DefaultCurrency);
        var money2 = new Money(5.25m, DefaultCurrency);

        // Act
        var result = money1 + money2;

        // Assert
        result.Amount.Should().Be(15.75m);
        result.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void AdditionOperator_WithZeroAmount_ShouldReturnCorrectSum()
    {
        // Arrange
        var money1 = new Money(10.50m, DefaultCurrency);
        var money2 = Money.Zero(DefaultCurrency);

        // Act
        var result = money1 + money2;

        // Assert
        result.Amount.Should().Be(10.50m);
        result.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void AdditionOperator_WithNegativeAmount_ShouldCalculateCorrectly()
    {
        // Arrange
        var money1 = new Money(10.00m, DefaultCurrency);
        var money2 = new Money(-3.50m, DefaultCurrency);

        // Act
        var result = money1 + money2;

        // Assert
        result.Amount.Should().Be(6.50m);
        result.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void AdditionOperator_WithDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(10.50m, DefaultCurrency);
        var money2 = new Money(5.25m, AlternateCurrency);

        // Act & Assert
        Action act = () => { var result = money1 + money2; };
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Cannot add money with different currencies*");
    }

    #endregion

    #region Subtraction Operator Tests

    [Test]
    public void SubtractionOperator_WithSameCurrency_ShouldSubtractAmountsCorrectly()
    {
        // Arrange
        var money1 = new Money(10.50m, DefaultCurrency);
        var money2 = new Money(3.25m, DefaultCurrency);

        // Act
        var result = money1 - money2;

        // Assert
        result.Amount.Should().Be(7.25m);
        result.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void SubtractionOperator_WithZeroAmount_ShouldReturnCorrectDifference()
    {
        // Arrange
        var money1 = new Money(10.50m, DefaultCurrency);
        var money2 = Money.Zero(DefaultCurrency);

        // Act
        var result = money1 - money2;

        // Assert
        result.Amount.Should().Be(10.50m);
        result.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void SubtractionOperator_WithNegativeAmount_ShouldCalculateCorrectly()
    {
        // Arrange
        var money1 = new Money(10.00m, DefaultCurrency);
        var money2 = new Money(-2.50m, DefaultCurrency);

        // Act
        var result = money1 - money2;

        // Assert
        result.Amount.Should().Be(12.50m);
        result.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void SubtractionOperator_ResultingInNegative_ShouldCalculateCorrectly()
    {
        // Arrange
        var money1 = new Money(5.00m, DefaultCurrency);
        var money2 = new Money(8.00m, DefaultCurrency);

        // Act
        var result = money1 - money2;

        // Assert
        result.Amount.Should().Be(-3.00m);
        result.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void SubtractionOperator_WithDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var money1 = new Money(10.50m, DefaultCurrency);
        var money2 = new Money(5.25m, AlternateCurrency);

        // Act & Assert
        Action act = () => { var result = money1 - money2; };
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Cannot subtract money with different currencies*");
    }

    #endregion

    #region Multiplication Operator Tests

    [Test]
    public void MultiplicationOperator_WithPositiveMultiplier_ShouldMultiplyCorrectly()
    {
        // Arrange
        var money = new Money(10.50m, DefaultCurrency);
        var multiplier = 2.5m;

        // Act
        var result = money * multiplier;

        // Assert
        result.Amount.Should().Be(26.25m);
        result.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void MultiplicationOperator_WithZeroMultiplier_ShouldReturnZero()
    {
        // Arrange
        var money = new Money(10.50m, DefaultCurrency);
        var multiplier = 0m;

        // Act
        var result = money * multiplier;

        // Assert
        result.Amount.Should().Be(0m);
        result.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void MultiplicationOperator_WithNegativeMultiplier_ShouldReturnNegativeAmount()
    {
        // Arrange
        var money = new Money(10.00m, DefaultCurrency);
        var multiplier = -1.5m;

        // Act
        var result = money * multiplier;

        // Assert
        result.Amount.Should().Be(-15.00m);
        result.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void MultiplicationOperator_WithDecimalMultiplier_ShouldHandlePrecisionCorrectly()
    {
        // Arrange
        var money = new Money(10.33m, DefaultCurrency);
        var multiplier = 1.5m;

        // Act
        var result = money * multiplier;

        // Assert
        result.Amount.Should().Be(15.50m);
        result.Currency.Should().Be(DefaultCurrency);
    }

    #endregion

    #region ToString() Method Tests

    [Test]
    public void ToString_WithDecimalAmount_ShouldFormatCorrectly()
    {
        // Arrange
        var money = new Money(10.50m, DefaultCurrency);

        // Act
        var result = money.ToString();

        // Assert
        result.Should().Be("10.50 USD");
    }

    [Test]
    public void ToString_WithWholeNumber_ShouldShowTwoDecimalPlaces()
    {
        // Arrange
        var money = new Money(25m, DefaultCurrency);

        // Act
        var result = money.ToString();

        // Assert
        result.Should().Be("25.00 USD");
    }

    [Test]
    public void ToString_WithZeroAmount_ShouldFormatCorrectly()
    {
        // Arrange
        var money = Money.Zero(DefaultCurrency);

        // Act
        var result = money.ToString();

        // Assert
        result.Should().Be("0.00 USD");
    }

    [Test]
    public void ToString_WithNegativeAmount_ShouldFormatCorrectly()
    {
        // Arrange
        var money = new Money(-15.75m, DefaultCurrency);

        // Act
        var result = money.ToString();

        // Assert
        result.Should().Be("-15.75 USD");
    }

    [Test]
    public void ToString_WithDifferentCurrency_ShouldShowCorrectCurrency()
    {
        // Arrange
        var money = new Money(100.00m, AlternateCurrency);

        // Act
        var result = money.ToString();

        // Assert
        result.Should().Be("100.00 EUR");
    }

    #endregion

    #region Equality Tests

    [Test]
    public void Equals_WithSameAmountAndCurrency_ShouldReturnTrue()
    {
        // Arrange
        var money1 = new Money(10.50m, DefaultCurrency);
        var money2 = new Money(10.50m, DefaultCurrency);

        // Act & Assert
        money1.Should().Be(money2);
        (money1 == money2).Should().BeTrue();
        (money1 != money2).Should().BeFalse();
    }

    [Test]
    public void Equals_WithDifferentAmounts_ShouldReturnFalse()
    {
        // Arrange
        var money1 = new Money(10.50m, DefaultCurrency);
        var money2 = new Money(15.50m, DefaultCurrency);

        // Act & Assert
        money1.Should().NotBe(money2);
        (money1 == money2).Should().BeFalse();
        (money1 != money2).Should().BeTrue();
    }

    [Test]
    public void Equals_WithDifferentCurrencies_ShouldReturnFalse()
    {
        // Arrange
        var money1 = new Money(10.50m, DefaultCurrency);
        var money2 = new Money(10.50m, AlternateCurrency);

        // Act & Assert
        money1.Should().NotBe(money2);
        (money1 == money2).Should().BeFalse();
        (money1 != money2).Should().BeTrue();
    }

    [Test]
    public void GetHashCode_WithSameAmountAndCurrency_ShouldReturnSameHashCode()
    {
        // Arrange
        var money1 = new Money(10.50m, DefaultCurrency);
        var money2 = new Money(10.50m, DefaultCurrency);

        // Act & Assert
        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }

    [Test]
    public void GetHashCode_WithDifferentAmountOrCurrency_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var money1 = new Money(10.50m, DefaultCurrency);
        var money2 = new Money(15.50m, DefaultCurrency);
        var money3 = new Money(10.50m, AlternateCurrency);

        // Act & Assert
        money1.GetHashCode().Should().NotBe(money2.GetHashCode());
        money1.GetHashCode().Should().NotBe(money3.GetHashCode());
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Test]
    public void Constructor_WithMaxDecimalValue_ShouldWork()
    {
        // Arrange & Act
        var money = new Money(decimal.MaxValue, DefaultCurrency);

        // Assert
        money.Amount.Should().Be(decimal.MaxValue);
        money.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void Constructor_WithMinDecimalValue_ShouldWork()
    {
        // Arrange & Act
        var money = new Money(decimal.MinValue, DefaultCurrency);

        // Assert
        money.Amount.Should().Be(decimal.MinValue);
        money.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void AdditionOperator_WithLargeNumbers_ShouldNotOverflow()
    {
        // Arrange
        var money1 = new Money(decimal.MaxValue / 2, DefaultCurrency);
        var money2 = new Money(decimal.MaxValue / 4, DefaultCurrency);

        // Act
        var result = money1 + money2;

        // Assert
        result.Amount.Should().Be(decimal.MaxValue / 2 + decimal.MaxValue / 4);
        result.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void MultiplicationOperator_WithVerySmallAmount_ShouldMaintainPrecision()
    {
        // Arrange
        var money = new Money(0.01m, DefaultCurrency);
        var multiplier = 0.1m;

        // Act
        var result = money * multiplier;

        // Assert
        result.Amount.Should().Be(0.00m);
        result.Currency.Should().Be(DefaultCurrency);
    }

    #endregion

    #region MoneyExtensions Tests

    [Test]
    public void Sum_WithValidMoneyList_ShouldReturnCorrectSum()
    {
        // Arrange
        var items = new[]
        {
            new { Price = new Money(10.50m, DefaultCurrency) },
            new { Price = new Money(25.75m, DefaultCurrency) },
            new { Price = new Money(5.25m, DefaultCurrency) }
        };

        // Act
        var result = items.Sum(item => item.Price, DefaultCurrency);

        // Assert
        result.Amount.Should().Be(41.50m);
        result.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void Sum_WithEmptyList_ShouldReturnZero()
    {
        // Arrange
        var items = Array.Empty<object>();

        // Act
        var result = items.Sum(_ => new Money(0, DefaultCurrency), DefaultCurrency);

        // Assert
        result.Amount.Should().Be(0m);
        result.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void Sum_WithSingleItem_ShouldReturnThatItem()
    {
        // Arrange
        var expectedAmount = 15.75m;
        var items = new[]
        {
            new { Price = new Money(expectedAmount, DefaultCurrency) }
        };

        // Act
        var result = items.Sum(item => item.Price, DefaultCurrency);

        // Assert
        result.Amount.Should().Be(expectedAmount);
        result.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void Sum_WithNegativeAmounts_ShouldCalculateCorrectly()
    {
        // Arrange
        var items = new[]
        {
            new { Price = new Money(10.00m, DefaultCurrency) },
            new { Price = new Money(-5.50m, DefaultCurrency) },
            new { Price = new Money(2.25m, DefaultCurrency) }
        };

        // Act
        var result = items.Sum(item => item.Price, DefaultCurrency);

        // Assert
        result.Amount.Should().Be(6.75m);
        result.Currency.Should().Be(DefaultCurrency);
    }

    [Test]
    public void Sum_WithComplexSelector_ShouldWorkCorrectly()
    {
        // Arrange
        var orderItems = new[]
        {
            new { Quantity = 2, UnitPrice = new Money(10.50m, DefaultCurrency) },
            new { Quantity = 3, UnitPrice = new Money(7.25m, DefaultCurrency) },
            new { Quantity = 1, UnitPrice = new Money(15.00m, DefaultCurrency) }
        };

        // Act
        var result = orderItems.Sum(item => item.UnitPrice * item.Quantity, DefaultCurrency);

        // Assert
        // (2 * 10.50) + (3 * 7.25) + (1 * 15.00) = 21.00 + 21.75 + 15.00 = 57.75
        result.Amount.Should().Be(57.75m);
        result.Currency.Should().Be(DefaultCurrency);
    }

    #endregion
}
