using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Orders.Commands.InitiateOrder;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;

/// <summary>
/// Tests for InitiateOrder command validation rules and input validation.
/// Focuses on FluentValidation rules and input validation scenarios.
/// </summary>
public class InitiateOrderValidationTests : InitiateOrderTestBase
{
    #region Required Field Validation Tests

    [Test]
    public async Task InitiateOrder_WithEmptyRestaurantId_ShouldFailValidation()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildInvalidCommand(InitiateOrderTestHelper.InvalidField.EmptyRestaurantId);

        // Act
        var action = async () => await SendAsync(command);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task InitiateOrder_WithEmptyItems_ShouldFailValidation()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildInvalidCommand(InitiateOrderTestHelper.InvalidField.EmptyItems);

        // Act
        var action = async () => await SendAsync(command);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task InitiateOrder_WithNullDeliveryAddress_ShouldFailValidation()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildInvalidCommand(InitiateOrderTestHelper.InvalidField.NullDeliveryAddress);

        // Act
        var action = async () => await SendAsync(command);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task InitiateOrder_WithInvalidPaymentMethod_ShouldFailValidation()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildInvalidCommand(InitiateOrderTestHelper.InvalidField.InvalidPaymentMethod);

        // Act
        var action = async () => await SendAsync(command);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
    }

    #endregion

    #region Item Quantity Validation Tests

    [Test]
    public async Task InitiateOrder_WithZeroQuantity_ShouldFailValidation()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildInvalidCommand(InitiateOrderTestHelper.InvalidField.ZeroQuantity);

        // Act
        var action = async () => await SendAsync(command);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task InitiateOrder_WithNegativeQuantity_ShouldFailValidation()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildInvalidCommand(InitiateOrderTestHelper.InvalidField.NegativeQuantity);

        // Act
        var action = async () => await SendAsync(command);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task InitiateOrder_WithExcessiveQuantity_ShouldFailValidation()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildInvalidCommand(InitiateOrderTestHelper.InvalidField.ExcessiveQuantity);

        // Act
        var action = async () => await SendAsync(command);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
    }

    #endregion

    #region Order Size Validation Tests

    [Test]
    public async Task InitiateOrder_WithTooManyItems_ShouldFailValidation()
    {
        // Arrange
        var items = Enumerable.Range(1, 51) // Create 51 items (exceeds max of 50)
            .Select(i => new OrderItemDto(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1))
            .ToList();

        var command = InitiateOrderTestHelper.BuildValidCommand()
            with
        { Items = items };

        // Act
        var action = async () => await SendAsync(command);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
    }

    #endregion

    #region Address Validation Tests

    [Test]
    public async Task InitiateOrder_WithInvalidDeliveryAddress_ShouldFailValidation()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildInvalidCommand(InitiateOrderTestHelper.InvalidField.InvalidAddress);

        // Act
        var action = async () => await SendAsync(command);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task InitiateOrder_WithTooLongAddressFields_ShouldFailValidation()
    {
        // Arrange
        var invalidAddress = new DeliveryAddressDto(
            Street: new string('A', 201), // Exceeds 200 char limit
            City: new string('B', 101),   // Exceeds 100 char limit
            State: new string('C', 101),  // Exceeds 100 char limit
            ZipCode: new string('D', 21), // Exceeds 20 char limit
            Country: new string('E', 101) // Exceeds 100 char limit
        );

        var command = InitiateOrderTestHelper.BuildValidCommand()
            with
        { DeliveryAddress = invalidAddress };

        // Act
        var action = async () => await SendAsync(command);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
    }

    #endregion

    #region Optional Field Validation Tests

    [Test]
    public async Task InitiateOrder_WithNegativeTip_ShouldFailValidation()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildInvalidCommand(InitiateOrderTestHelper.InvalidField.NegativeTip);

        // Act
        var action = async () => await SendAsync(command);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task InitiateOrder_WithTooLongSpecialInstructions_ShouldFailValidation()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildInvalidCommand(InitiateOrderTestHelper.InvalidField.TooLongSpecialInstructions);

        // Act
        var action = async () => await SendAsync(command);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task InitiateOrder_WithTooLongCouponCode_ShouldFailValidation()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand()
            with
        { CouponCode = new string('X', 51) }; // Exceeds 50 char limit

        // Act
        var action = async () => await SendAsync(command);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
    }

    #endregion

    #region Valid Edge Cases Tests

    [Test]
    public async Task InitiateOrder_WithNullOptionalFields_ShouldPassValidation()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand(
            couponCode: null,
            tipAmount: null,
            specialInstructions: null
        );

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    [Test]
    public async Task InitiateOrder_WithEmptyOptionalStrings_ShouldPassValidation()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand(
            couponCode: string.Empty,
            specialInstructions: string.Empty
        );

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    [Test]
    public async Task InitiateOrder_WithZeroTipAmount_ShouldPassValidation()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand(tipAmount: 0.00m);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    [Test]
    public async Task InitiateOrder_WithMaximumAllowedValues_ShouldPassValidation()
    {
        // Arrange
        var maxItems = Enumerable.Range(1, 50) // Maximum allowed items
            .Select(i => new OrderItemDto(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 10)) // Max quantity per item
            .ToList();

        var maxAddress = new DeliveryAddressDto(
            Street: new string('A', 200),     // Max street length
            City: new string('B', 100),       // Max city length
            State: new string('C', 100),      // Max state length
            ZipCode: new string('D', 20),     // Max zip code length
            Country: new string('E', 100)     // Max country length
        );

        var command = InitiateOrderTestHelper.BuildValidCommand()
            with
        {
            Items = maxItems,
            DeliveryAddress = maxAddress,
            CouponCode = null, // Use no coupon to focus on field length validation
            SpecialInstructions = new string('Y', 500) // Max special instructions length
        };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    #endregion

    #region Payment Method Validation Tests

    [Test]
    [TestCase("CreditCard")]
    [TestCase("PayPal")]
    [TestCase("ApplePay")]
    [TestCase("GooglePay")]
    [TestCase("CashOnDelivery")]
    public async Task InitiateOrder_WithValidPaymentMethods_ShouldPassValidation(string paymentMethod)
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand(paymentMethod: paymentMethod);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    [Test]
    [TestCase("creditcard")]     // lowercase
    [TestCase("CREDITCARD")]     // uppercase
    [TestCase("CrEdItCaRd")]     // mixed case
    [TestCase("paypal")]         // lowercase
    [TestCase("PAYPAL")]         // uppercase
    [TestCase("cashondelivery")] // lowercase
    public async Task InitiateOrder_WithCaseInsensitivePaymentMethods_ShouldPassValidation(string paymentMethod)
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand(paymentMethod: paymentMethod);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    [Test]
    [TestCase("Bitcoin")]
    [TestCase("Check")]
    [TestCase("InvalidMethod")]
    [TestCase("")]
    [TestCase(" ")]
    public async Task InitiateOrder_WithInvalidPaymentMethods_ShouldFailValidation(string invalidPaymentMethod)
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand(paymentMethod: invalidPaymentMethod);

        // Act
        var action = async () => await SendAsync(command);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
    }

    #endregion
}
