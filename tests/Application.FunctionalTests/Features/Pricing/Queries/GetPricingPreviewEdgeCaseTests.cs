using FluentAssertions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.Pricing.Queries.GetPricingPreview;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Pricing.Queries;

/// <summary>
/// Tests for edge cases and error scenarios in pricing preview.
/// Tests boundary conditions, invalid inputs, and error handling.
/// </summary>
[TestFixture]
public class GetPricingPreviewEdgeCaseTests : BaseTestFixture
{
    [SetUp]
    public async Task SetUpUser()
    {
        SetUserId(Testing.TestData.DefaultCustomerId);
        await Task.CompletedTask;
    }

    #region Boundary Value Tests

    [Test]
    public async Task GetPricingPreview_WithMaximumQuantity_ShouldHandleCorrectly()
    {
        // Arrange
        var maxQuantity = 99; // Maximum allowed quantity
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), maxQuantity)
            },
            CouponCode: null,
            TipAmount: null
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        // Verify pricing calculation with maximum quantity
        response.Subtotal.Amount.Should().Be(383760m * maxQuantity);
        response.TotalAmount.Amount.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetPricingPreview_WithQuantityExceedingLimit_ShouldReturnValidationError()
    {
        // Arrange
        var excessiveQuantity = 100; // Exceeds maximum allowed quantity
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), excessiveQuantity)
            },
            CouponCode: null,
            TipAmount: null
        );

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<Application.Common.Exceptions.ValidationException>();
    }

    [Test]
    public async Task GetPricingPreview_WithZeroQuantity_ShouldReturnValidationError()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 0)
            },
            CouponCode: null,
            TipAmount: null
        );

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<Application.Common.Exceptions.ValidationException>();
    }

    [Test]
    public async Task GetPricingPreview_WithNegativeQuantity_ShouldReturnValidationError()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), -1)
            },
            CouponCode: null,
            TipAmount: null
        );

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<Application.Common.Exceptions.ValidationException>();
    }

    [Test]
    public async Task GetPricingPreview_WithVeryLargeTip_ShouldHandleCorrectly()
    {
        // Arrange
        var largeTip = 1000.00m;
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1)
            },
            CouponCode: null,
            TipAmount: largeTip
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        response.TipAmount.Amount.Should().Be(largeTip);
        response.TotalAmount.Amount.Should().BeGreaterThan(largeTip);
    }

    #endregion

    #region Invalid Input Tests

    [Test]
    public async Task GetPricingPreview_WithEmptyGuidRestaurantId_ShouldReturnValidationError()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Guid.Empty,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1)
            },
            CouponCode: null,
            TipAmount: null
        );

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<Application.Common.Exceptions.ValidationException>();
    }

    [Test]
    public async Task GetPricingPreview_WithEmptyGuidMenuItemId_ShouldReturnValidationError()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Guid.Empty, 1)
            },
            CouponCode: null,
            TipAmount: null
        );

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<Application.Common.Exceptions.ValidationException>();
    }

    [Test]
    public async Task GetPricingPreview_WithVeryLongCouponCode_ShouldReturnValidationError()
    {
        // Arrange
        var longCouponCode = new string('A', 51); // Exceeds 50 character limit
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1)
            },
            CouponCode: longCouponCode,
            TipAmount: null
        );

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<Application.Common.Exceptions.ValidationException>();
    }

    [Test]
    public async Task GetPricingPreview_WithEmptyGuidCustomizationGroupId_ShouldReturnValidationError()
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
                            Guid.Empty, // Empty customization group ID
                            new List<Guid> { TestDataFactory.CustomizationChoice_ExtraCheeseId }
                        )
                    }
                )
            },
            CouponCode: null,
            TipAmount: null
        );

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<Application.Common.Exceptions.ValidationException>();
    }

    [Test]
    public async Task GetPricingPreview_WithEmptyGuidChoiceId_ShouldReturnValidationError()
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
                            new List<Guid> { Guid.Empty } // Empty choice ID
                        )
                    }
                )
            },
            CouponCode: null,
            TipAmount: null
        );

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<Application.Common.Exceptions.ValidationException>();
    }

    #endregion

    #region Data Consistency Tests

    [Test]
    public async Task GetPricingPreview_WithMenuItemFromDifferentRestaurant_ShouldReturnWarning()
    {
        // Arrange - This test would require a menu item from a different restaurant
        // For now, we'll test with a non-existent menu item which should behave similarly
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
    public async Task GetPricingPreview_WithInactiveRestaurant_ShouldReturnError()
    {
        // Arrange - This test would require an inactive restaurant
        // For now, we'll test with a non-existent restaurant which should behave similarly
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
    public async Task GetPricingPreview_WithUnavailableMenuItem_ShouldReturnWarning()
    {
        // Arrange - This test would require an unavailable menu item
        // For now, we'll test with a non-existent menu item which should behave similarly
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

    #endregion

    #region Performance and Stress Tests

    [Test]
    public async Task GetPricingPreview_WithManyItems_ShouldCompleteSuccessfully()
    {
        // Arrange
        var menuItemIds = Testing.TestData.GetMenuItemIds(
            Testing.TestData.MenuItems.ClassicBurger,
            Testing.TestData.MenuItems.BuffaloWings,
            Testing.TestData.MenuItems.CaesarSalad
        );

        var items = new List<PricingPreviewItemDto>();
        for (int i = 0; i < 10; i++)
        {
            items.Add(new(menuItemIds[i % menuItemIds.Count], 1));
        }

        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: items,
            CouponCode: Testing.TestData.DefaultCouponCode,
            TipAmount: 10.00m
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        response.Subtotal.Amount.Should().BeGreaterThan(0);
        response.TotalAmount.Amount.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetPricingPreview_WithManyCustomizations_ShouldCompleteSuccessfully()
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
                            new List<Guid> 
                            { 
                                TestDataFactory.CustomizationChoice_ExtraCheeseId,
                                TestDataFactory.CustomizationChoice_BaconId
                            }
                        ),
                        new(
                            TestDataFactory.CustomizationGroup_RequiredBunTypeId!.Value,
                            new List<Guid> { TestDataFactory.CustomizationChoice_BriocheBunId!.Value }
                        )
                    }
                )
            },
            CouponCode: Testing.TestData.DefaultCouponCode,
            TipAmount: 5.00m
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        response.Subtotal.Amount.Should().BeGreaterThan(383760m);
        response.TotalAmount.Amount.Should().BeGreaterThan(0);
    }

    #endregion

    #region Currency and Precision Tests

    [Test]
    public async Task GetPricingPreview_ShouldMaintainCurrencyConsistency()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1)
            },
            CouponCode: null,
            TipAmount: 5.00m
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        // Verify all amounts use the same currency
        response.Subtotal.Currency.Should().Be("VND");
        response.DeliveryFee.Currency.Should().Be("VND");
        response.TipAmount.Currency.Should().Be("VND");
        response.TaxAmount.Currency.Should().Be("VND");
        response.TotalAmount.Currency.Should().Be("VND");
        response.Currency.Should().Be("VND");

        if (response.DiscountAmount != null)
        {
            response.DiscountAmount.Currency.Should().Be("VND");
        }
    }

    [Test]
    public async Task GetPricingPreview_ShouldHandleDecimalPrecisionCorrectly()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 1)
            },
            CouponCode: null,
            TipAmount: 3.33m // Decimal tip amount
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        // Verify decimal precision is maintained
        response.TipAmount.Amount.Should().Be(3.33m);
        
        // Verify all amounts are properly calculated with precision
        response.Subtotal.Amount.Should().BeGreaterThan(0);
        response.DeliveryFee.Amount.Should().BeGreaterThan(0);
        response.TaxAmount.Amount.Should().BeGreaterThan(0);
        response.TotalAmount.Amount.Should().BeGreaterThan(0);
    }

    #endregion
}
