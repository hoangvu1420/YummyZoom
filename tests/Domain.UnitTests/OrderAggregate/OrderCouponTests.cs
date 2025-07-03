using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.OrderAggregate;

/// <summary>
/// Tests for Order aggregate coupon-related functionality including applying and removing coupons.
/// </summary>
[TestFixture]
public class OrderCouponTests
{
    private static readonly UserId DefaultCustomerId = UserId.CreateUnique();
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private static readonly DeliveryAddress DefaultDeliveryAddress = CreateDefaultDeliveryAddress();
    private static readonly List<OrderItem> DefaultOrderItems = CreateDefaultOrderItems();
    private const string DefaultSpecialInstructions = "No special instructions";
    private static readonly Money DefaultDiscountAmount = Money.Zero;
    private static readonly Money DefaultDeliveryFee = new Money(5.00m);
    private static readonly Money DefaultTipAmount = new Money(2.00m);
    private static readonly Money DefaultTaxAmount = new Money(1.50m);

    #region ApplyCoupon() Method Tests

    [Test]
    public void ApplyCoupon_WithValidPercentageCoupon_ShouldSucceedAndApplyDiscount()
    {
        // Arrange
        var order = CreateValidOrder();
        var coupon = CreatePercentageCoupon(0.10m); // 10% discount
        var expectedDiscount = new Money(order.Subtotal.Amount * 0.10m);

        // Act
        var result = order.ApplyCoupon(coupon);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(expectedDiscount);
        order.AppliedCouponIds.Should().ContainSingle();
        order.AppliedCouponIds.Should().Contain((CouponId)coupon.Id);
        order.LastUpdateTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        // Verify total amount is recalculated
        var expectedTotal = new Money(order.Subtotal.Amount - expectedDiscount.Amount + 
                                    order.TaxAmount.Amount + order.DeliveryFee.Amount + order.TipAmount.Amount);
        order.TotalAmount.Should().Be(expectedTotal);
    }

    [Test]
    public void ApplyCoupon_WithValidFixedAmountCoupon_ShouldSucceedAndApplyDiscount()
    {
        // Arrange
        var order = CreateValidOrder();
        var discountAmount = new Money(5.00m);
        var coupon = CreateFixedAmountCoupon(discountAmount);

        // Act
        var result = order.ApplyCoupon(coupon);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(discountAmount);
        order.AppliedCouponIds.Should().ContainSingle();
        order.AppliedCouponIds.Should().Contain((CouponId)coupon.Id);
    }

    [Test]
    public void ApplyCoupon_WithFixedAmountExceedingSubtotal_ShouldCapDiscountToSubtotal()
    {
        // Arrange
        var order = CreateValidOrder();
        var largeDiscountAmount = new Money(1000.00m); // Much larger than subtotal
        var coupon = CreateFixedAmountCoupon(largeDiscountAmount);

        // Act
        var result = order.ApplyCoupon(coupon);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(order.Subtotal); // Capped to subtotal
    }

    [Test]
    public void ApplyCoupon_WithValidFreeItemCoupon_ShouldSucceedAndApplyDiscount()
    {
        // Arrange
        var order = CreateValidOrder();
        var freeItemId = order.OrderItems.First().Snapshot_MenuItemId;
        var coupon = CreateFreeItemCoupon(freeItemId);
        var expectedDiscount = order.OrderItems.First().LineItemTotal;

        // Act
        var result = order.ApplyCoupon(coupon);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(expectedDiscount);
        order.AppliedCouponIds.Should().ContainSingle();
    }

    [Test]
    public void ApplyCoupon_WithCouponForSpecificCategory_ShouldApplyToMatchingItems()
    {
        // Arrange
        var order = CreateValidOrder();
        var categoryId = order.OrderItems.First().Snapshot_MenuCategoryId;
        var coupon = CreateCategorySpecificCoupon(categoryId, 0.15m); // 15% discount
        var expectedDiscount = new Money(order.OrderItems
            .Where(oi => oi.Snapshot_MenuCategoryId == categoryId)
            .Sum(oi => oi.LineItemTotal.Amount) * 0.15m);

        // Act
        var result = order.ApplyCoupon(coupon);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(expectedDiscount);
    }

    [Test]
    public void ApplyCoupon_WithMinOrderAmountNotMet_ShouldFailWithNotApplicableError()
    {
        // Arrange
        var order = CreateValidOrder();
        var minOrderAmount = new Money(1000.00m); // Much higher than order subtotal
        var coupon = CreatePercentageCouponWithMinOrder(0.10m, minOrderAmount);

        // Act
        var result = order.ApplyCoupon(coupon);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.CouponNotApplicable);
        order.DiscountAmount.Should().Be(Money.Zero);
        order.AppliedCouponIds.Should().BeEmpty();
    }

    [Test]
    public void ApplyCoupon_WhenCouponAlreadyApplied_ShouldFailWithAlreadyAppliedError()
    {
        // Arrange
        var order = CreateValidOrder();
        var coupon1 = CreatePercentageCoupon(0.10m);
        var coupon2 = CreatePercentageCoupon(0.15m);
        order.ApplyCoupon(coupon1);

        // Act
        var result = order.ApplyCoupon(coupon2);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.CouponAlreadyApplied);
        order.AppliedCouponIds.Should().ContainSingle(); // Still only the first coupon
    }

    [Test]
    public void ApplyCoupon_WhenOrderNotInPlacedStatus_ShouldFailWithInvalidStatusError()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Accept(DateTime.UtcNow.AddHours(1)); // Move to Accepted status
        var coupon = CreatePercentageCoupon(0.10m);

        // Act
        var result = order.ApplyCoupon(coupon);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.CouponCannotBeAppliedToOrderStatus);
        order.DiscountAmount.Should().Be(Money.Zero);
        order.AppliedCouponIds.Should().BeEmpty();
    }

    [Test]
    public void ApplyCoupon_WithCouponForNonMatchingItems_ShouldFailWithNotApplicableError()
    {
        // Arrange
        var order = CreateValidOrder();
        var nonMatchingItemId = MenuItemId.CreateUnique(); // Different from order items
        var coupon = CreateItemSpecificCoupon(nonMatchingItemId, 0.20m);

        // Act
        var result = order.ApplyCoupon(coupon);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.CouponNotApplicable);
        order.DiscountAmount.Should().Be(Money.Zero);
    }

    [Test]
    public void ApplyCoupon_FollowedByRemoveCoupon_ShouldRestoreOriginalTotals()
    {
        // Arrange
        var order = CreateValidOrder();
        var originalDiscountAmount = order.DiscountAmount;
        var originalTotalAmount = order.TotalAmount;
        var coupon = CreatePercentageCoupon(0.20m); // 20% discount

        // Act - Apply coupon
        order.ApplyCoupon(coupon);
        
        // Verify coupon is applied
        order.DiscountAmount.Should().NotBe(originalDiscountAmount);
        order.TotalAmount.Should().NotBe(originalTotalAmount);
        
        // Act - Remove coupon
        var result = order.RemoveCoupon();

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(originalDiscountAmount);
        order.TotalAmount.Should().Be(originalTotalAmount);
        order.AppliedCouponIds.Should().BeEmpty();
    }

    [Test]
    public void ApplyCoupon_WithVerySmallPercentageDiscount_ShouldSucceedWithMinimalDiscount()
    {
        // Arrange
        var order = CreateValidOrder();
        var coupon = CreateCategorySpecificCoupon(order.OrderItems.First().Snapshot_MenuCategoryId, 0.01m); // 0.01% discount
        var discountBaseAmount = order.OrderItems
            .Where(oi => oi.Snapshot_MenuCategoryId == order.OrderItems.First().Snapshot_MenuCategoryId)
            .Sum(oi => oi.LineItemTotal.Amount);
        var expectedDiscount = new Money(discountBaseAmount * 0.01m);

        // Act
        var result = order.ApplyCoupon(coupon);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(expectedDiscount);
        order.AppliedCouponIds.Should().ContainSingle();
    }

    #endregion

    #region RemoveCoupon() Method Tests

    [Test]
    public void RemoveCoupon_WithAppliedCoupon_ShouldSucceedAndRemoveDiscount()
    {
        // Arrange
        var order = CreateValidOrder();
        var coupon = CreatePercentageCoupon(0.10m);
        order.ApplyCoupon(coupon);
        var originalTotal = order.TotalAmount;

        // Act
        var result = order.RemoveCoupon();

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(Money.Zero);
        order.AppliedCouponIds.Should().BeEmpty();
        order.LastUpdateTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        // Verify total amount is recalculated without discount
        var expectedTotal = new Money(order.Subtotal.Amount + 
                                    order.TaxAmount.Amount + order.DeliveryFee.Amount + order.TipAmount.Amount);
        order.TotalAmount.Should().Be(expectedTotal);
    }

    [Test]
    public void RemoveCoupon_WithNoCouponApplied_ShouldSucceedWithoutChanges()
    {
        // Arrange
        var order = CreateValidOrder();
        var originalDiscountAmount = order.DiscountAmount;
        var originalTotalAmount = order.TotalAmount;
        var originalAppliedCoupons = order.AppliedCouponIds.Count;

        // Act
        var result = order.RemoveCoupon();

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(originalDiscountAmount);
        order.TotalAmount.Should().Be(originalTotalAmount);
        order.AppliedCouponIds.Should().HaveCount(originalAppliedCoupons);
    }

    #endregion

    #region Helper Methods

    private static Order CreateValidOrder()
    {
        return Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            DefaultDiscountAmount,
            DefaultDeliveryFee,
            DefaultTipAmount,
            DefaultTaxAmount).Value;
    }

    private static DeliveryAddress CreateDefaultDeliveryAddress()
    {
        return DeliveryAddress.Create(
            "123 Main St",
            "Springfield",
            "IL",
            "62701",
            "USA").Value;
    }

    private static List<OrderItem> CreateDefaultOrderItems()
    {
        var orderItem = OrderItem.Create(
            MenuCategoryId.CreateUnique(),
            MenuItemId.CreateUnique(),
            "Test Item",
            new Money(10.00m),
            2).Value;

        return new List<OrderItem> { orderItem };
    }

    private static Coupon CreatePercentageCoupon(decimal percentage)
    {
        var couponValue = CouponValue.CreatePercentage(percentage).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;
        
        return Coupon.Create(
            RestaurantId.CreateUnique(),
            "TEST10",
            "Test percentage coupon",
            couponValue,
            appliesTo,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(30)).Value;
    }

    private static Coupon CreateFixedAmountCoupon(Money amount)
    {
        var couponValue = CouponValue.CreateFixedAmount(amount).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;
        
        return Coupon.Create(
            RestaurantId.CreateUnique(),
            "SAVE5",
            "Test fixed amount coupon",
            couponValue,
            appliesTo,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(30)).Value;
    }

    private static Coupon CreateFreeItemCoupon(MenuItemId freeItemId)
    {
        var couponValue = CouponValue.CreateFreeItem(freeItemId).Value;
        var appliesTo = AppliesTo.CreateForSpecificItems(new List<MenuItemId> { freeItemId }).Value;
        
        return Coupon.Create(
            RestaurantId.CreateUnique(),
            "FREEITEM",
            "Test free item coupon",
            couponValue,
            appliesTo,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(30)).Value;
    }

    private static Coupon CreateCategorySpecificCoupon(MenuCategoryId categoryId, decimal percentage)
    {
        var couponValue = CouponValue.CreatePercentage(percentage).Value;
        var appliesTo = AppliesTo.CreateForSpecificCategories(new List<MenuCategoryId> { categoryId }).Value;
        
        return Coupon.Create(
            RestaurantId.CreateUnique(),
            "CATEGORY15",
            "Test category-specific coupon",
            couponValue,
            appliesTo,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(30)).Value;
    }

    private static Coupon CreateItemSpecificCoupon(MenuItemId itemId, decimal percentage)
    {
        var couponValue = CouponValue.CreatePercentage(percentage).Value;
        var appliesTo = AppliesTo.CreateForSpecificItems(new List<MenuItemId> { itemId }).Value;
        
        return Coupon.Create(
            RestaurantId.CreateUnique(),
            "ITEM20",
            "Test item-specific coupon",
            couponValue,
            appliesTo,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(30)).Value;
    }

    private static Coupon CreatePercentageCouponWithMinOrder(decimal percentage, Money minOrderAmount)
    {
        var couponValue = CouponValue.CreatePercentage(percentage).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;
        
        return Coupon.Create(
            RestaurantId.CreateUnique(),
            "MINORDER",
            "Test coupon with minimum order amount",
            couponValue,
            appliesTo,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(30),
            minOrderAmount).Value;
    }

    #endregion
}
