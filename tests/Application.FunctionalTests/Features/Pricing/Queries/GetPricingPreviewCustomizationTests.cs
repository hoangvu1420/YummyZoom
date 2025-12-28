using FluentAssertions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.Pricing.Queries.GetPricingPreview;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Pricing.Queries;

/// <summary>
/// Specialized tests for customization handling in pricing preview.
/// Tests various customization scenarios including valid customizations, invalid choices, and pricing calculations.
/// </summary>
[TestFixture]
public class GetPricingPreviewCustomizationTests : BaseTestFixture
{
    [SetUp]
    public async Task SetUpUser()
    {
        SetUserId(Testing.TestData.DefaultCustomerId);
        await Task.CompletedTask;
    }

    [Test]
    public async Task GetPricingPreview_WithSingleCustomization_ShouldIncludeInPricing()
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
        response.Notes.Should().NotContain(n => n.Code == "CUSTOMIZATION_INVALID");
    }

    [Test]
    public async Task GetPricingPreview_WithMultipleCustomizations_ShouldIncludeAllInPricing()
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

        // Verify pricing includes all customization costs
        response.Subtotal.Amount.Should().BeGreaterThan(383760m); // Base price of Classic Burger

        // Verify no error notes
        response.Notes.Should().NotContain(n => n.Type == "error");
    }

    [Test]
    public async Task GetPricingPreview_WithMultipleCustomizationGroups_ShouldHandleCorrectly()
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
                        ),
                        new(
                            TestDataFactory.CustomizationGroup_RequiredBunTypeId!.Value,
                            new List<Guid> { TestDataFactory.CustomizationChoice_BriocheBunId!.Value }
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

        // Verify pricing includes all customization costs
        response.Subtotal.Amount.Should().BeGreaterThan(383760m); // Base price of Classic Burger

        // Verify no error notes
        response.Notes.Should().NotContain(n => n.Type == "error");
    }

    [Test]
    public async Task GetPricingPreview_WithInvalidCustomizationGroup_ShouldReturnError()
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
        result.ShouldBeFailure("PricingPreview.NoValidItems");
    }

    [Test]
    public async Task GetPricingPreview_WithInvalidChoiceId_ShouldReturnError()
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
                            new List<Guid> { Guid.NewGuid() } // Invalid choice ID
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
        result.ShouldBeFailure("PricingPreview.NoValidItems");
    }

    [Test]
    public async Task GetPricingPreview_WithEmptyChoiceIds_ShouldReturnError()
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
                            new List<Guid>() // Empty choice IDs
                        )
                    }
                )
            },
            CouponCode: null,
            TipAmount: null
        );

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ValidationException>();
    }

    [Test]
    public async Task GetPricingPreview_WithDuplicateChoiceIds_ShouldReturnError()
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
                                TestDataFactory.CustomizationChoice_ExtraCheeseId // Duplicate choice ID
                            }
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
        result.ShouldBeFailure("PricingPreview.NoValidItems");
    }

    [Test]
    public async Task GetPricingPreview_WithCustomizationNotApplicableToMenuItem_ShouldReturnError()
    {
        // Arrange - Try to apply burger customizations to a non-burger item
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(
                    Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.CaesarSalad), // Salad item
                    1,
                    new List<PricingPreviewCustomizationDto>
                    {
                        new(
                            TestDataFactory.CustomizationGroup_BurgerAddOnsId, // Burger customization
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
        result.ShouldBeFailure("PricingPreview.NoValidItems");
    }

    [Test]
    public async Task GetPricingPreview_WithNoCustomizations_ShouldUseBasePrice()
    {
        // Arrange
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(
                    Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 
                    1,
                    null // No customizations
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

        // Verify pricing uses base price only
        response.Subtotal.Amount.Should().Be(383760m); // Base price of Classic Burger

        // Verify no error notes
        response.Notes.Should().NotContain(n => n.Type == "error");
    }

    [Test]
    public async Task GetPricingPreview_WithCustomizationsAndQuantity_ShouldCalculateCorrectly()
    {
        // Arrange
        var quantity = 2;
        var query = new GetPricingPreviewQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Items: new List<PricingPreviewItemDto>
            {
                new(
                    Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger), 
                    quantity,
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

        // Verify pricing calculation includes quantity
        // Base price: 383,760 VND * 2 = 767,520 VND
        // Plus customization costs * quantity
        response.Subtotal.Amount.Should().BeGreaterThan(767520m);

        // Verify no error notes
        response.Notes.Should().NotContain(n => n.Type == "error");
    }

    [Test]
    public async Task GetPricingPreview_WithCustomizationsAndCoupon_ShouldApplyBoth()
    {
        // Arrange - Create a coupon with appropriate minimum amount for Classic Burger + Extra Cheese
        // Classic Burger (383,760) + Extra Cheese (36,000) = 419,760 VND
        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions
        {
            Code = "CUSTOMIZATION_TEST",
            MinimumOrderAmount = 400000m, // 400,000 VND - below the total with customization
            DiscountPercentage = 10m,
            Description = "Test coupon for customization scenarios"
        });

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
            CouponCode: couponCode,
            TipAmount: null
        );

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;

        // Verify both customization and coupon are applied
        response.Subtotal.Amount.Should().BeGreaterThan(383760m); // Includes customization
        response.DiscountAmount.Should().NotBeNull(); // Coupon applied

        // Verify both notes are present
        response.Notes.Should().Contain(n => n.Code == "COUPON_APPLIED");
        response.Notes.Should().NotContain(n => n.Type == "error");
    }
}
