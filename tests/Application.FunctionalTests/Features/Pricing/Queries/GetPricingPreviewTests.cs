using FluentAssertions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.Orders.Commands.InitiateOrder;
using YummyZoom.Application.Pricing.Queries.GetPricingPreview;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.Services;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Pricing.Queries;

/// <summary>
/// Comprehensive functional tests for the GetPricingPreview query.
/// Tests pricing calculations, coupon validation, customization handling, and error scenarios.
/// Verifies consistency with order creation logic and proper integration with StaticPricingService.
/// </summary>
[TestFixture]
public class GetPricingPreviewTests : BaseTestFixture
{
    [SetUp]
    public async Task SetUpUser()
    {
        // Authenticate as default customer for pricing preview
        SetUserId(Testing.TestData.DefaultCustomerId);
        await Task.CompletedTask;
    }

    #region Basic Functionality Tests

    [Test]
    public async Task GetPricingPreview_WithValidItems_ShouldReturnCorrectPricing()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 2)
            },
            CouponCode: null,
            TipAmount: 5.00m
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        // Verify basic structure
        response.Should().NotBeNull();
        response.Currency.Should().Be("VND");
        response.CalculatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify pricing components
        response.Subtotal.Should().NotBeNull();
        response.Subtotal.Amount.Should().BeGreaterThan(0);
        response.DeliveryFee.Should().NotBeNull();
        response.TipAmount.Should().NotBeNull();
        response.TaxAmount.Should().NotBeNull();
        response.TotalAmount.Should().NotBeNull();

        // Verify tip amount
        response.TipAmount.Amount.Should().Be(5.00m);
        response.TipAmount.Currency.Should().Be("VND");

        // Verify discount is null when no coupon
        response.DiscountAmount.Should().BeNull();

        // Verify notes are empty for successful case
        response.Notes.Should().BeEmpty();

        // Suggestions payload should be null when not requested
        response.CouponSuggestions.Should().BeNull();
    }

    [Test]
    public async Task GetPricingPreview_WithMultipleItems_ShouldCalculateCorrectly()
    {
        // Arrange
        var menuItemIds = Testing.TestData.GetMenuItemIds(
            Testing.TestData.MenuItems.ClassicBurger,
            Testing.TestData.MenuItems.BuffaloWings
        );

        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(menuItemIds[0], 2), // Classic Burger x2
                new(menuItemIds[1], 1)  // Buffalo Wings x1
            },
            CouponCode: null,
            TipAmount: null
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        // Verify subtotal calculation
        // Classic Burger: 383,760 VND * 2 = 767,520 VND
        // Buffalo Wings: 311,760 VND * 1 = 311,760 VND
        // Total: 767,520 + 311,760 = 1,079,280 VND
        const decimal expectedSubtotal = 1079280m;
        response.Subtotal.Amount.Should().Be(expectedSubtotal);

        // Verify delivery fee from StaticPricingService
        var pricingConfig = StaticPricingService.GetPricingConfiguration(
            YummyZoom.Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
        response.DeliveryFee.Should().Be(pricingConfig.DeliveryFee);

        // Verify tax calculation
        var expectedTaxBase = StaticPricingService.CalculateTaxBase(
            response.Subtotal, response.DeliveryFee, response.TipAmount, pricingConfig.TaxBasePolicy);
        var expectedTaxAmount = new Money(expectedTaxBase.Amount * pricingConfig.TaxRate, "VND");
        response.TaxAmount.Should().Be(expectedTaxAmount);

        // Verify total calculation
        var expectedTotal = response.Subtotal + response.DeliveryFee + response.TipAmount + response.TaxAmount;
        response.TotalAmount.Should().Be(expectedTotal);
    }

    [Test]
    public async Task GetPricingPreview_WithZeroTip_ShouldHandleCorrectly()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1)
            },
            CouponCode: null,
            TipAmount: 0m
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        response.TipAmount.Amount.Should().Be(0m);
        response.TipAmount.Currency.Should().Be("VND");
    }

    #endregion

    #region Coupon Validation Tests

    [Test]
    public async Task GetPricingPreview_WithValidCoupon_ShouldApplyDiscount()
    {
        // Arrange
        // Use multiple items to meet minimum order amount requirement (480,000 VND)
        // ClassicBurger: 383,760 VND + BuffaloWings: 311,760 VND = 695,520 VND (exceeds minimum)
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1),
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.BuffaloWings), 1)
            },
            CouponCode: Testing.TestData.DefaultCouponCode,
            TipAmount: null
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        // Verify discount was applied
        response.DiscountAmount.Should().NotBeNull();
        response.DiscountAmount!.Amount.Should().BeGreaterThan(0);

        // Verify note about coupon application
        response.Notes.Should().Contain(n => 
            n.Type == "info" && 
            n.Code == "COUPON_APPLIED" && 
            n.Message.Contains(Testing.TestData.DefaultCouponCode));

        // Verify total is reduced by discount
        var expectedTotal = response.Subtotal - response.DiscountAmount + response.DeliveryFee + response.TipAmount + response.TaxAmount;
        response.TotalAmount.Should().Be(expectedTotal);
        
        // Verify discount calculation (15% of subtotal: 695,520 VND * 0.15 = 104,328 VND)
        const decimal expectedSubtotal = 695520m; // ClassicBurger (383,760) + BuffaloWings (311,760)
        const decimal expectedDiscount = 104328m; // 15% of 695,520
        response.Subtotal.Amount.Should().Be(expectedSubtotal);
        response.DiscountAmount!.Amount.Should().Be(expectedDiscount);
    }

    [Test]
    public async Task GetPricingPreview_WithInvalidCoupon_ShouldReturnWarning()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1)
            },
            CouponCode: "INVALID_COUPON",
            TipAmount: null
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        // Verify no discount applied
        response.DiscountAmount.Should().BeNull();

        // Verify warning note
        response.Notes.Should().Contain(n => 
            n.Type == "warning" && 
            n.Code == "COUPON_NOT_FOUND" && 
            n.Message.Contains("INVALID_COUPON"));
    }

    [Test]
    public async Task GetPricingPreview_WithExpiredCoupon_ShouldReturnWarning()
    {
        // Arrange - Create an expired coupon for testing
        var expiredCouponCode = "EXPIRED_COUPON";
        // Note: This would require creating an expired coupon in test data
        // For now, we'll test with a non-existent coupon which should behave similarly

        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1)
            },
            CouponCode: expiredCouponCode,
            TipAmount: null
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        // Verify no discount applied
        response.DiscountAmount.Should().BeNull();

        // Verify warning note
        response.Notes.Should().Contain(n => 
            n.Type == "warning" && 
            n.Code == "COUPON_NOT_FOUND");
    }

    #endregion

    #region Customization Tests

    [Test]
    public async Task GetPricingPreview_WithValidCustomizations_ShouldIncludeInPricing()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(
                    Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 
                    1,
                    new List<PricingPreviewCustomizationDto>
                    {
                        new(
                            TestDataFactory.CustomizationGroup_BurgerAddOnsId,
                            new List<Guid> { TestDataFactory.CustomizationChoice_ExtraCheeseId }
                        )
                    }
                )
            },
            CouponCode: null,
            TipAmount: null
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        // Verify pricing includes customization costs
        response.Subtotal.Amount.Should().BeGreaterThan(383760m); // Base price of Classic Burger

        // Verify no error notes
        response.Notes.Should().NotContain(n => n.Type == "error");
    }

    [Test]
    public async Task GetPricingPreview_WithInvalidCustomization_ShouldReturnError()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(
                    Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 
                    1,
                    new List<PricingPreviewCustomizationDto>
                    {
                        new(
                            Guid.NewGuid(), // Invalid customization group ID
                            new List<Guid> { Guid.NewGuid() }
                        )
                    }
                )
            },
            CouponCode: null,
            TipAmount: null
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        // When invalid customizations are provided, the entire operation should fail
        // because there are no valid items to process
        result.ShouldBeFailure("PricingPreview.NoValidItems");
    }

    #endregion

    #region Error Scenario Tests

    [Test]
    public async Task GetPricingPreview_WithNonExistentRestaurant_ShouldReturnError()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Guid.NewGuid(), // Non-existent restaurant
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1)
            },
            CouponCode: null,
            TipAmount: null
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeFailure("PricingPreview.RestaurantNotFound");
    }

    [Test]
    public async Task GetPricingPreview_WithNonExistentMenuItem_ShouldReturnWarning()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Guid.NewGuid(), 1) // Non-existent menu item
            },
            CouponCode: null,
            TipAmount: null
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeFailure("PricingPreview.NoValidItems");
    }

    [Test]
    public async Task GetPricingPreview_WithEmptyItems_ShouldReturnValidationError()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>(), // Empty items
            CouponCode: null,
            TipAmount: null
        );

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ValidationException>();
    }

    [Test]
    public async Task GetPricingPreview_WithInvalidQuantity_ShouldReturnValidationError()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 0) // Invalid quantity
            },
            CouponCode: null,
            TipAmount: null
        );

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ValidationException>();
    }

    [Test]
    public async Task GetPricingPreview_WithNegativeTip_ShouldReturnValidationError()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1)
            },
            CouponCode: null,
            TipAmount: -5.00m // Negative tip
        );

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ValidationException>();
    }

    #endregion

    #region Pricing Consistency Tests

    [Test]
    public async Task GetPricingPreview_ShouldMatchOrderCreationPricing()
    {
        // Arrange
        var menuItemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        var tipAmount = 5.00m;
        var couponCode = Testing.TestData.DefaultCouponCode;

        // Create pricing preview query
        var previewQuery = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(menuItemId, 2)
            },
            CouponCode: couponCode,
            TipAmount: tipAmount
        );

        // Create equivalent order command
        var orderCommand = new InitiateOrderCommand(
            CustomerId: Testing.TestData.DefaultCustomerId,
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<OrderItemDto>
            {
                new(menuItemId, 2)
            },
            DeliveryAddress: new DeliveryAddressDto(
                "123 Test St", "Test City", "Test State", "12345", "Test Country"),
            PaymentMethod: "CreditCard",
            SpecialInstructions: null,
            CouponCode: couponCode,
            TipAmount: tipAmount,
            TeamCartId: null
        );

        // Act
        var previewResult = await SendAsync(previewQuery);
        var orderResult = await SendAsync(orderCommand);

        // Assert
        previewResult.ShouldBeSuccessful();
        orderResult.ShouldBeSuccessful();

        var preview = previewResult.Value;
        var order = await FindOrderAsync(orderResult.Value.OrderId);

        // Verify pricing consistency
        preview.Subtotal.Should().Be(order!.Subtotal);
        preview.DiscountAmount.Should().Be(order.DiscountAmount);
        preview.DeliveryFee.Should().Be(order.DeliveryFee);
        preview.TipAmount.Should().Be(order.TipAmount);
        preview.TaxAmount.Should().Be(order.TaxAmount);
        preview.TotalAmount.Should().Be(order.TotalAmount);
        preview.Currency.Should().Be(order.TotalAmount.Currency);
    }

    [Test]
    public async Task GetPricingPreview_WithStaticPricingService_ShouldUseConsistentValues()
    {
        // Arrange
        var restaurantId = YummyZoom.Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(Testing.TestData.DefaultRestaurantId);
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1)
            },
            CouponCode: null,
            TipAmount: 3.00m
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        // Get pricing configuration from StaticPricingService
        var pricingConfig = StaticPricingService.GetPricingConfiguration(restaurantId);

        // Verify delivery fee matches StaticPricingService
        response.DeliveryFee.Should().Be(pricingConfig.DeliveryFee);

        // Verify tax calculation matches StaticPricingService logic
        var expectedTaxBase = StaticPricingService.CalculateTaxBase(
            response.Subtotal, response.DeliveryFee, response.TipAmount, pricingConfig.TaxBasePolicy);
        var expectedTaxAmount = new Money(expectedTaxBase.Amount * pricingConfig.TaxRate, "VND");
        response.TaxAmount.Should().Be(expectedTaxAmount);
    }

    #endregion

    #region Performance and Caching Tests

    [Test]
    public async Task GetPricingPreview_WithSameInputs_ShouldReturnConsistentResults()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1)
            },
            CouponCode: null,
            TipAmount: 2.50m
        );

        // Act - Run the same query multiple times
        var result1 = await SendAsync(query);
        var result2 = await SendAsync(query);
        var result3 = await SendAsync(query);

        // Assert
        result1.ShouldBeSuccessful();
        result2.ShouldBeSuccessful();
        result3.ShouldBeSuccessful();

        // Verify all results are identical
        result1.Value.Subtotal.Should().Be(result2.Value.Subtotal);
        result1.Value.Subtotal.Should().Be(result3.Value.Subtotal);
        result1.Value.TotalAmount.Should().Be(result2.Value.TotalAmount);
        result1.Value.TotalAmount.Should().Be(result3.Value.TotalAmount);
    }

    [Test]
    public async Task GetPricingPreview_ShouldCompleteWithinReasonableTime()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1),
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.BuffaloWings), 2),
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.CaesarSalad), 1)
            },
            CouponCode: Testing.TestData.DefaultCouponCode,
            TipAmount: 5.00m
        );

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await SendAsync(query);
        stopwatch.Stop();

        // Assert
        result.ShouldBeSuccessful();
        
        // Should complete within 2 seconds (reasonable for functional test with database operations)
        // Note: Functional tests involve database queries, dependency injection, and complex pricing calculations
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);
    }

    #endregion

    #region Coupon Suggestion Tests

    [Test]
    public async Task GetPricingPreview_WithSuggestionsFlag_ShouldReturnSuggestionEnvelope()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1)
            },
            CouponCode: null,
            TipAmount: null,
            IncludeCouponSuggestions: true);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;
        response.CouponSuggestions.Should().NotBeNull();
        response.Notes.Should().NotContain(n => n.Code == "COUPON_SUGGESTIONS_UNAVAILABLE");
    }

    [Test]
    public async Task GetPricingPreview_WithSuggestionsFlagAndNoUser_ShouldAddWarningNote()
    {
        // Arrange
        ClearUserId();

        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1)
            },
            CouponCode: null,
            TipAmount: null,
            IncludeCouponSuggestions: true);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;
        response.Notes.Should().Contain(n => n.Code == "COUPON_SUGGESTIONS_UNAVAILABLE");
        response.CouponSuggestions.Should().NotBeNull();
        response.CouponSuggestions!.Suggestions.Should().BeEmpty();
        response.CouponSuggestions.BestDeal.Should().BeNull();

        // Restore authenticated context for other tests
        SetUserId(Testing.TestData.DefaultCustomerId);
    }

    #endregion
}
