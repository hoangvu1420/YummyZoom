using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.Orders.Commands.InitiateOrder;
using YummyZoom.Domain.CouponAggregate.Errors;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;

/// <summary>
/// Tests for InitiateOrder command business rules and domain logic.
/// Focuses on restaurant validation, menu item validation, and coupon business rule enforcement.
/// Creates test scenarios that require custom data setup and tests error conditions and messages.
/// </summary>
public class InitiateOrderBusinessRuleTests : InitiateOrderTestBase
{
    #region Restaurant Validation Tests

    [Test]
    public async Task InitiateOrder_WithNonExistentRestaurant_ShouldFailWithRestaurantNotFound()
    {
        // Arrange
        var nonExistentRestaurantId = Guid.NewGuid();
        var command = InitiateOrderTestHelper.BuildValidCommand(
            restaurantId: nonExistentRestaurantId);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(InitiateOrderErrors.RestaurantNotFound());
    }

    [Test]
    public async Task InitiateOrder_WithRestaurant_ShouldFailWithRestaurantNotActive()
    {
        // Arrange - Create an inactive restaurant (not verified or accepting orders)
        var inactiveRestaurantId = await TestDataFactory.CreateInactiveRestaurantAsync();
        var command = InitiateOrderTestHelper.BuildValidCommand(
            restaurantId: inactiveRestaurantId);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(InitiateOrderErrors.RestaurantNotActive());
    }

    #endregion

    #region Menu Item Validation Tests

    [Test]
    public async Task InitiateOrder_WithNonExistentMenuItem_ShouldFailWithMenuItemsNotFound()
    {
        // Arrange
        var nonExistentMenuItemId = Guid.NewGuid();
        var command = InitiateOrderTestHelper.BuildValidCommand(
            menuItemIds: new List<Guid> { nonExistentMenuItemId });

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(InitiateOrderErrors.MenuItemsNotFound());
    }

    [Test]
    public async Task InitiateOrder_WithMenuItemsFromDifferentRestaurant_ShouldFailWithMenuItemsNotFromRestaurant()
    {
        // Arrange - Create a second restaurant with its own menu items
        var secondRestaurantData = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();

        // Try to order items from second restaurant but specify first restaurant as target
        var command = InitiateOrderTestHelper.BuildValidCommand(
            restaurantId: Testing.TestData.DefaultRestaurantId, // First restaurant
            menuItemIds: new List<Guid> { secondRestaurantData.MenuItemId }); // Second restaurant's item

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(InitiateOrderErrors.MenuItemsNotFromRestaurant());
    }

    [Test]
    public async Task InitiateOrder_WithUnavailableMenuItem_ShouldFailWithMenuItemNotAvailable()
    {
        // Arrange - Mark a menu item as unavailable
        var unavailableItemName = await TestDataFactory.MarkMenuItemAsUnavailableAsync(Testing.TestData.MenuItems.FreshJuice);
        var unavailableItemId = Testing.TestData.GetMenuItemId(unavailableItemName);

        var command = InitiateOrderTestHelper.BuildValidCommand(
            menuItemIds: new List<Guid> { unavailableItemId });

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(InitiateOrderErrors.MenuItemNotAvailable(unavailableItemName));
    }

    #endregion

    #region Coupon Validation Tests

    [Test]
    public async Task InitiateOrder_WithNonExistentCoupon_ShouldFailWithCouponNotFound()
    {
        // Arrange
        var nonExistentCouponCode = "NONEXISTENT";
        var command = InitiateOrderTestHelper.BuildValidCommand(
            couponCode: nonExistentCouponCode);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(InitiateOrderErrors.CouponNotFound(nonExistentCouponCode));
    }

    [Test]
    public async Task InitiateOrder_WithExpiredCoupon_ShouldFailWithCouponExpired()
    {
        // Arrange - Create an expired coupon
        var expiredCouponCode = await CouponTestDataFactory.CreateExpiredCouponAsync();
        var command = InitiateOrderTestHelper.BuildValidCommand(
            couponCode: expiredCouponCode);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CouponErrors.CouponExpired);
    }

    [Test]
    public async Task InitiateOrder_WithDisabledCoupon_ShouldFailWithCouponDisabled()
    {
        // Arrange - Create a disabled coupon
        var disabledCouponCode = await CouponTestDataFactory.CreateDisabledCouponAsync();
        var command = InitiateOrderTestHelper.BuildValidCommand(
            couponCode: disabledCouponCode);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CouponErrors.CouponDisabled);
    }

    [Test]
    public async Task InitiateOrder_WithCouponExceedingUsageLimit_ShouldFailWithUsageLimitExceeded()
    {
        // Arrange - Create a coupon with limited usage and exhaust its limit
        var limitedCouponCode = await CouponTestDataFactory.CreateCouponWithUsageLimitAsync(totalLimit: 2);

        // Use the coupon twice to reach its limit
        await UseCouponAsync(limitedCouponCode);
        await UseCouponAsync(limitedCouponCode);

        // Try to use it a third time
        var command = InitiateOrderTestHelper.BuildValidCommand(
            couponCode: limitedCouponCode);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CouponErrors.UsageLimitExceeded);
    }

    [Test]
    public async Task InitiateOrder_WithCouponExceedingUserUsageLimit_ShouldFailWithUserUsageLimitExceeded()
    {
        // Arrange - Create a coupon with per-user usage limit
        var userLimitedCouponCode = await CouponTestDataFactory.CreateCouponWithUserUsageLimitAsync(userLimit: 1);

        // Use the coupon once for the current user
        await UseCouponAsync(userLimitedCouponCode);

        // Try to use it again by the same user
        var command = InitiateOrderTestHelper.BuildValidCommand(
            couponCode: userLimitedCouponCode);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CouponErrors.UserUsageLimitExceeded);
    }

    [Test]
    public async Task InitiateOrder_WithCouponBelowMinimumOrder_ShouldFailWithMinAmountNotMet()
    {
        // Arrange - Create a coupon with high minimum order amount
        var minOrderCouponCode = await CouponTestDataFactory.CreateCouponWithMinimumOrderAmountAsync(minimumAmount: 100.00m);

        // Create order with low value (default items are around $15-16)
        var command = InitiateOrderTestHelper.BuildValidCommand(
            menuItemIds: new List<Guid> { Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger) },
            couponCode: minOrderCouponCode);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CouponErrors.MinAmountNotMet);
    }

    [Test]
    public async Task InitiateOrder_WithCouponNotApplicableToItems_ShouldFailWithNotApplicable()
    {
        // Arrange - Create a coupon that applies only to specific items not in the order
        var specificItemCouponCode = await CouponTestDataFactory.CreateCouponForSpecificItemAsync(
            Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.MargheritaPizza));

        // Order different items that the coupon doesn't apply to
        var command = InitiateOrderTestHelper.BuildValidCommand(
            menuItemIds: new List<Guid> { Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger) },
            couponCode: specificItemCouponCode);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CouponErrors.NotApplicable);
    }

    [Test]
    public async Task InitiateOrder_WithNotYetValidCoupon_ShouldFailWithCouponNotYetValid()
    {
        // Arrange - Create a future-dated coupon
        var futureCouponCode = await CouponTestDataFactory.CreateFutureCouponAsync();
        var command = InitiateOrderTestHelper.BuildValidCommand(
            couponCode: futureCouponCode);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(CouponErrors.CouponNotYetValid);
    }

    #endregion

    #region Cross-Entity Validation Tests

    [Test]
    public async Task InitiateOrder_WithMultipleMenuItemsFromDifferentRestaurants_ShouldFailWithMenuItemsNotFromRestaurant()
    {
        // Arrange - Create second restaurant and get items from both restaurants
        var secondRestaurantData = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        var firstRestaurantItemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);

        var command = InitiateOrderTestHelper.BuildValidCommand(
            restaurantId: Testing.TestData.DefaultRestaurantId,
            menuItemIds: new List<Guid> { firstRestaurantItemId, secondRestaurantData.MenuItemId });

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(InitiateOrderErrors.MenuItemsNotFromRestaurant());
    }

    [Test]
    public async Task InitiateOrder_WithCouponFromDifferentRestaurant_ShouldFailWithCouponNotFound()
    {
        // Arrange - Create a coupon for a different restaurant
        var secondRestaurantData = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        var differentRestaurantCouponCode = await CouponTestDataFactory.CreateCouponForRestaurantAsync(secondRestaurantData.RestaurantId);

        // Try to use it for the default restaurant
        var command = InitiateOrderTestHelper.BuildValidCommand(
            restaurantId: Testing.TestData.DefaultRestaurantId,
            couponCode: differentRestaurantCouponCode);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(InitiateOrderErrors.CouponNotFound(differentRestaurantCouponCode));
    }

    #endregion

    #region Test Data Setup Helper Methods

    private async Task UseCouponAsync(string couponCode)
    {
        // Create a simple order to consume the coupon usage
        var command = InitiateOrderTestHelper.BuildValidCommand(
            couponCode: couponCode,
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery); // Use COD to avoid payment complexity

        var result = await SendAsync(command);
        result.ShouldBeSuccessful(); // Ensure the usage was successful
    }

    #endregion
}
