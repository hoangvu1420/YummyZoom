using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.CouponAggregate;

/// <summary>
/// Tests for Coupon validation functionality including AppliesToItem() method and business rule validation.
/// </summary>
[TestFixture]
public class CouponValidationTests
{
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private const string DefaultCode = "SAVE10";
    private const string DefaultDescription = "Save 10% on your order";
    private static readonly CouponValue DefaultValue = CouponValue.CreatePercentage(10m).Value;
    private static readonly DateTime DefaultStartDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DefaultEndDate = new(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);

    #region AppliesToItem() Method Tests

    [Test]
    public void AppliesToItem_WithWholeOrderCoupon_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;
        var coupon = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            appliesTo,
            DefaultStartDate,
            DefaultEndDate).Value;

        var menuItemId = MenuItemId.CreateUnique();
        var categoryId = MenuCategoryId.CreateUnique();

        // Act
        var result = coupon.AppliesToItem(menuItemId, categoryId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void AppliesToItem_WithSpecificItemCoupon_ShouldReturnTrueForMatchingItem()
    {
        // Arrange
        var menuItemId = MenuItemId.CreateUnique();
        var appliesTo = AppliesTo.CreateForSpecificItems([menuItemId]).Value;
        var coupon = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            appliesTo,
            DefaultStartDate,
            DefaultEndDate).Value;

        var categoryId = MenuCategoryId.CreateUnique();

        // Act
        var result = coupon.AppliesToItem(menuItemId, categoryId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void AppliesToItem_WithSpecificItemCoupon_ShouldReturnFalseForDifferentItem()
    {
        // Arrange
        var targetMenuItemId = MenuItemId.CreateUnique();
        var differentMenuItemId = MenuItemId.CreateUnique();
        var appliesTo = AppliesTo.CreateForSpecificItems([targetMenuItemId]).Value;
        var coupon = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            appliesTo,
            DefaultStartDate,
            DefaultEndDate).Value;

        var categoryId = MenuCategoryId.CreateUnique();

        // Act
        var result = coupon.AppliesToItem(differentMenuItemId, categoryId);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void AppliesToItem_WithCategoryCoupon_ShouldReturnTrueForItemInMatchingCategory()
    {
        // Arrange
        var categoryId = MenuCategoryId.CreateUnique();
        var appliesTo = AppliesTo.CreateForSpecificCategories([categoryId]).Value;
        var coupon = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            appliesTo,
            DefaultStartDate,
            DefaultEndDate).Value;

        var menuItemId = MenuItemId.CreateUnique();

        // Act
        var result = coupon.AppliesToItem(menuItemId, categoryId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void AppliesToItem_WithCategoryCoupon_ShouldReturnFalseForItemInDifferentCategory()
    {
        // Arrange
        var targetCategoryId = MenuCategoryId.CreateUnique();
        var differentCategoryId = MenuCategoryId.CreateUnique();
        var appliesTo = AppliesTo.CreateForSpecificCategories([targetCategoryId]).Value;
        var coupon = Coupon.Create(
            DefaultRestaurantId,
            DefaultCode,
            DefaultDescription,
            DefaultValue,
            appliesTo,
            DefaultStartDate,
            DefaultEndDate).Value;

        var menuItemId = MenuItemId.CreateUnique();

        // Act
        var result = coupon.AppliesToItem(menuItemId, differentCategoryId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
