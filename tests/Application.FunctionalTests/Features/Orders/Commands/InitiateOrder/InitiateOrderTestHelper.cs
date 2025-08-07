using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Commands.InitiateOrder;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;

/// <summary>
/// Test helper class providing utilities for InitiateOrder command testing.
/// Focuses on command construction, mocking setup, and validation helpers.
/// </summary>
public static class InitiateOrderTestHelper
{
    #region Command Builders
    
    /// <summary>
    /// Builds a valid InitiateOrder command with default test data.
    /// </summary>
    public static InitiateOrderCommand BuildValidCommand(
        Guid? customerId = null,
        Guid? restaurantId = null,
        List<Guid>? menuItemIds = null,
        string paymentMethod = PaymentMethods.CreditCard,
        string? couponCode = null,
        decimal? tipAmount = null,
        string? specialInstructions = null,
        Guid? teamCartId = null)
    {
        var items = (menuItemIds ?? GetDefaultMenuItemIds())
            .Select(id => new OrderItemDto(id, 1))
            .ToList();

        return new InitiateOrderCommand(
            CustomerId: customerId ?? Testing.TestData.DefaultCustomerId,
            RestaurantId: restaurantId ?? Testing.TestData.DefaultRestaurantId,
            Items: items,
            DeliveryAddress: DefaultDeliveryAddress,
            PaymentMethod: paymentMethod,
            SpecialInstructions: specialInstructions,
            CouponCode: couponCode,
            TipAmount: tipAmount,
            TeamCartId: teamCartId
        );
    }

    /// <summary>
    /// Builds a valid command with a coupon applied.
    /// </summary>
    public static InitiateOrderCommand BuildValidCommandWithCoupon(
        string? couponCode = null,
        string paymentMethod = PaymentMethods.CreditCard)
    {
        return BuildValidCommand(
            couponCode: couponCode ?? Testing.TestData.DefaultCouponCode,
            paymentMethod: paymentMethod
        );
    }

    /// <summary>
    /// Builds a valid command with tip amount.
    /// </summary>
    public static InitiateOrderCommand BuildValidCommandWithTip(
        decimal tipAmount,
        string paymentMethod = PaymentMethods.CreditCard)
    {
        return BuildValidCommand(
            tipAmount: tipAmount,
            paymentMethod: paymentMethod
        );
    }

    /// <summary>
    /// Builds a command with invalid data for validation testing.
    /// </summary>
    public static InitiateOrderCommand BuildInvalidCommand(InvalidField invalidField)
    {
        var validCommand = BuildValidCommand();

        return invalidField switch
        {
            InvalidField.EmptyRestaurantId => validCommand with { RestaurantId = Guid.Empty },
            InvalidField.EmptyItems => validCommand with { Items = new List<OrderItemDto>() },
            InvalidField.NullDeliveryAddress => validCommand with { DeliveryAddress = null! },
            InvalidField.InvalidPaymentMethod => validCommand with { PaymentMethod = "InvalidMethod" },
            InvalidField.ZeroQuantity => validCommand with 
            { 
                Items = new List<OrderItemDto> { new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 0) }
            },
            InvalidField.NegativeQuantity => validCommand with 
            { 
                Items = new List<OrderItemDto> { new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), -1) }
            },
            InvalidField.ExcessiveQuantity => validCommand with 
            { 
                Items = new List<OrderItemDto> { new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 11) }
            },
            InvalidField.NegativeTip => validCommand with { TipAmount = -5.00m },
            InvalidField.TooLongSpecialInstructions => validCommand with 
            { 
                SpecialInstructions = new string('A', 501) 
            },
            InvalidField.InvalidAddress => validCommand with 
            { 
                DeliveryAddress = new DeliveryAddressDto("", "", "", "", "") 
            },
            _ => throw new ArgumentException($"Invalid field type not supported: {invalidField}")
        };
    }

    #endregion

    #region Mock Setup Helpers
    
    /// <summary>
    /// Configures IPaymentGatewayService mock for successful payment intent creation.
    /// </summary>
    public static Mock<IPaymentGatewayService> SetupSuccessfulPaymentGatewayMock()
    {
        var mock = new Mock<IPaymentGatewayService>();
        
        mock.Setup(x => x.CreatePaymentIntentAsync(
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(SharedKernel.Result.Success(new PaymentIntentResult(
                PaymentIntentId: $"pi_test_{Guid.NewGuid().ToString("N")[..16]}",
                ClientSecret: $"pi_test_{Guid.NewGuid().ToString("N")[..16]}_secret_{Guid.NewGuid().ToString("N")[..16]}"
            ))));

        return mock;
    }

    /// <summary>
    /// Configures IPaymentGatewayService mock to return failure.
    /// </summary>
    public static Mock<IPaymentGatewayService> SetupFailingPaymentGatewayMock(string errorMessage = "Payment gateway error")
    {
        var mock = new Mock<IPaymentGatewayService>();
        
        mock.Setup(x => x.CreatePaymentIntentAsync(
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(SharedKernel.Result.Failure<PaymentIntentResult>(Error.Failure("PaymentGateway.Error", errorMessage))));

        return mock;
    }

    /// <summary>
    /// Configures IPaymentGatewayService mock with custom response.
    /// </summary>
    public static Mock<IPaymentGatewayService> SetupPaymentGatewayMockWithCustomResponse(string paymentIntentId, string clientSecret)
    {
        var mock = new Mock<IPaymentGatewayService>();
        
        mock.Setup(x => x.CreatePaymentIntentAsync(
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(SharedKernel.Result.Success(new PaymentIntentResult(paymentIntentId, clientSecret))));

        return mock;
    }
    
    #endregion
    
    #region Assertion Helpers
    
    /// <summary>
    /// Validates that an order was created with expected financial calculations.
    /// </summary>
    public static void ValidateOrderFinancials(
        Order order,
        decimal? expectedSubtotal = null,
        decimal? expectedTax = null,
        decimal? expectedDeliveryFee = null,
        decimal? expectedDiscount = null,
        decimal? expectedTip = null,
        decimal? expectedTotal = null)
    {
        if (expectedSubtotal.HasValue)
            order.Subtotal.Amount.Should().Be(expectedSubtotal.Value);
        
        if (expectedTax.HasValue)
            order.TaxAmount.Amount.Should().Be(expectedTax.Value);
        
        if (expectedDeliveryFee.HasValue)
            order.DeliveryFee.Amount.Should().Be(expectedDeliveryFee.Value);
        
        if (expectedDiscount.HasValue)
            order.DiscountAmount.Amount.Should().Be(expectedDiscount.Value);
        
        if (expectedTip.HasValue)
            order.TipAmount.Amount.Should().Be(expectedTip.Value);
        
        if (expectedTotal.HasValue)
            order.TotalAmount.Amount.Should().Be(expectedTotal.Value);
    }

    /// <summary>
    /// Validates payment intent creation was called with correct parameters.
    /// </summary>
    public static void ValidatePaymentIntentCreation(
        Mock<IPaymentGatewayService> mock,
        decimal? expectedAmount = null,
        string? expectedCurrency = null,
        string[]? expectedMetadata = null,
        bool shouldBeCalled = true)
    {
        var times = shouldBeCalled ? Times.Once() : Times.Never();
        
        if (shouldBeCalled)
        {
            mock.Verify(x => x.CreatePaymentIntentAsync(
                expectedAmount.HasValue 
                    ? It.Is<Money>(m => m.Amount == expectedAmount.Value)
                    : It.IsAny<Money>(),
                expectedCurrency ?? It.IsAny<string>(),
                expectedMetadata != null 
                    ? It.Is<IDictionary<string, string>>(d => expectedMetadata.All(key => d.ContainsKey(key)))
                    : It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()), times);
        }
        else
        {
            mock.Verify(x => x.CreatePaymentIntentAsync(
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()), times);
        }
    }

    /// <summary>
    /// Validates that an order was properly persisted in the database.
    /// </summary>
    public static async Task<Order?> ValidateOrderPersistence(OrderId orderId, bool shouldExist = true)
    {
        var order = await FindOrderAsync(orderId);
        
        if (shouldExist)
        {
            order.Should().NotBeNull();
        }
        else
        {
            order.Should().BeNull();
        }
        
        return order;
    }
    
    #endregion
    
    #region Test Data
    
    /// <summary>
    /// Default delivery address for testing.
    /// </summary>
    public static DeliveryAddressDto DefaultDeliveryAddress { get; } = new(
        Street: "123 Test Street",
        City: "Test City",
        State: "TS",
        ZipCode: "12345",
        Country: "US"
    );

    /// <summary>
    /// Common test payment methods.
    /// </summary>
    public static class PaymentMethods
    {
        public const string CreditCard = "CreditCard";
        public const string PayPal = "PayPal";
        public const string ApplePay = "ApplePay";
        public const string GooglePay = "GooglePay";
        public const string CashOnDelivery = "CashOnDelivery";
    }

    /// <summary>
    /// Test amounts for common financial calculations.
    /// </summary>
    public static class TestAmounts
    {
        public static readonly decimal SmallTip = 2.00m;
        public static readonly decimal MediumTip = 5.00m;
        public static readonly decimal LargeTip = 10.00m;
        public static readonly decimal StandardDeliveryFee = 2.99m;
        public static readonly decimal TaxRate = 0.08m; // 8%
    }

    /// <summary>
    /// Invalid field types for validation testing.
    /// </summary>
    public enum InvalidField
    {
        EmptyRestaurantId,
        EmptyItems,
        NullDeliveryAddress,
        InvalidPaymentMethod,
        ZeroQuantity,
        NegativeQuantity,
        ExcessiveQuantity,
        NegativeTip,
        TooLongSpecialInstructions,
        InvalidAddress
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Gets default menu item IDs for testing.
    /// </summary>
    private static List<Guid> GetDefaultMenuItemIds()
    {
        return Testing.TestData.GetMenuItemIds(
            Testing.TestData.MenuItems.ClassicBurger,
            Testing.TestData.MenuItems.BuffaloWings
        );
    }

    #endregion
}
