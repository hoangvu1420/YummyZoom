using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
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
    private static readonly Money DefaultDiscountAmount = Money.Zero(Currencies.Default);
    private static readonly Money DefaultDeliveryFee = new Money(5.00m, Currencies.Default);
    private static readonly Money DefaultTipAmount = new Money(2.00m, Currencies.Default);
    private static readonly Money DefaultTaxAmount = new Money(1.50m, Currencies.Default);

    #region ApplyCoupon() Method Tests - New Decoupled Approach

    [Test]
    public void ApplyCoupon_WithValidPercentageCoupon_ShouldSucceedAndApplyDiscount()
    {
        // Arrange
        var order = CreateValidOrder();
        var couponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreatePercentage(10m).Value; // 10% discount
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;
        var expectedDiscount = new Money(order.Subtotal.Amount * 0.10m, Currencies.Default);

        // Act
        var result = order.ApplyCoupon(couponId, couponValue, appliesTo, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(expectedDiscount);
        order.AppliedCouponIds.Should().ContainSingle();
        order.AppliedCouponIds.Should().Contain(couponId);
        order.LastUpdateTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        // Verify total amount is recalculated
        var expectedTotal = new Money(order.Subtotal.Amount - expectedDiscount.Amount + 
                                    order.TaxAmount.Amount + order.DeliveryFee.Amount + order.TipAmount.Amount, Currencies.Default);
        order.TotalAmount.Should().Be(expectedTotal);
    }

    [Test]
    public void ApplyCoupon_WithValidFixedAmountCoupon_ShouldSucceedAndApplyDiscount()
    {
        // Arrange
        var order = CreateValidOrder();
        var couponId = CouponId.CreateUnique();
        var discountAmount = new Money(5.00m, Currencies.Default);
        var couponValue = CouponValue.CreateFixedAmount(discountAmount).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;

        // Act
        var result = order.ApplyCoupon(couponId, couponValue, appliesTo, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(discountAmount);
        order.AppliedCouponIds.Should().ContainSingle();
        order.AppliedCouponIds.Should().Contain(couponId);
    }

    [Test]
    public void ApplyCoupon_WithFixedAmountExceedingSubtotal_ShouldCapDiscountToSubtotal()
    {
        // Arrange
        var order = CreateValidOrder();
        var couponId = CouponId.CreateUnique();
        var largeDiscountAmount = new Money(1000.00m, Currencies.Default); // Much larger than subtotal
        var couponValue = CouponValue.CreateFixedAmount(largeDiscountAmount).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;

        // Act
        var result = order.ApplyCoupon(couponId, couponValue, appliesTo, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(order.Subtotal); // Capped to subtotal
    }

    [Test]
    public void ApplyCoupon_WithValidFreeItemCoupon_ShouldSucceedAndApplyDiscount()
    {
        // Arrange
        var order = CreateValidOrder();
        var couponId = CouponId.CreateUnique();
        var freeItemId = order.OrderItems.First().Snapshot_MenuItemId;
        var couponValue = CouponValue.CreateFreeItem(freeItemId).Value;
        var appliesTo = AppliesTo.CreateForSpecificItems(new List<MenuItemId> { freeItemId }).Value;
        
        // Expected discount should be the price of one unit of the item
        var expectedDiscount = new Money(
            order.OrderItems.First().LineItemTotal.Amount / order.OrderItems.First().Quantity, 
            Currencies.Default);

        // Act
        var result = order.ApplyCoupon(couponId, couponValue, appliesTo, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(expectedDiscount);
        order.AppliedCouponIds.Should().ContainSingle();
        order.AppliedCouponIds.Should().Contain(couponId);
    }

    [Test]
    public void ApplyCoupon_WithFreeItemNotInOrder_ShouldFailWithNotApplicableError()
    {
        // Arrange
        var order = CreateValidOrder();
        var couponId = CouponId.CreateUnique();
        var nonExistentItemId = MenuItemId.CreateUnique(); // Item not in the order
        var couponValue = CouponValue.CreateFreeItem(nonExistentItemId).Value;
        var appliesTo = AppliesTo.CreateForSpecificItems(new List<MenuItemId> { nonExistentItemId }).Value;

        // Act
        var result = order.ApplyCoupon(couponId, couponValue, appliesTo, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.CouponNotApplicable);
        order.DiscountAmount.Should().Be(Money.Zero(Currencies.Default));
        order.AppliedCouponIds.Should().BeEmpty();
    }

    [Test]
    public void ApplyCoupon_WithCouponForSpecificCategory_ShouldApplyToMatchingItems()
    {
        // Arrange
        var order = CreateValidOrder();
        var couponId = CouponId.CreateUnique();
        var categoryId = order.OrderItems.First().Snapshot_MenuCategoryId;
        var couponValue = CouponValue.CreatePercentage(15m).Value; // 15% discount
        var appliesTo = AppliesTo.CreateForSpecificCategories(new List<MenuCategoryId> { categoryId }).Value;
        
        var expectedDiscount = new Money(order.OrderItems
            .Where(oi => oi.Snapshot_MenuCategoryId == categoryId)
            .Sum(oi => oi.LineItemTotal.Amount) * 0.15m, Currencies.Default);

        // Act
        var result = order.ApplyCoupon(couponId, couponValue, appliesTo, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(expectedDiscount);
        order.AppliedCouponIds.Should().Contain(couponId);
    }

    [Test]
    public void ApplyCoupon_WithCouponForSpecificItems_ShouldApplyToMatchingItems()
    {
        // Arrange
        var order = CreateValidOrder();
        var couponId = CouponId.CreateUnique();
        var itemId = order.OrderItems.First().Snapshot_MenuItemId;
        var couponValue = CouponValue.CreatePercentage(20m).Value; // 20% discount
        var appliesTo = AppliesTo.CreateForSpecificItems(new List<MenuItemId> { itemId }).Value;
        
        var expectedDiscount = new Money(order.OrderItems
            .Where(oi => oi.Snapshot_MenuItemId == itemId)
            .Sum(oi => oi.LineItemTotal.Amount) * 0.20m, Currencies.Default);

        // Act
        var result = order.ApplyCoupon(couponId, couponValue, appliesTo, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(expectedDiscount);
        order.AppliedCouponIds.Should().Contain(couponId);
    }

    [Test]
    public void ApplyCoupon_WithMinOrderAmountNotMet_ShouldFailWithNotApplicableError()
    {
        // Arrange
        var order = CreateValidOrder();
        var couponId = CouponId.CreateUnique();
        var minOrderAmount = new Money(1000.00m, Currencies.Default); // Much higher than order subtotal
        var couponValue = CouponValue.CreatePercentage(10m).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;

        // Act
        var result = order.ApplyCoupon(couponId, couponValue, appliesTo, minOrderAmount);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.CouponNotApplicable);
        order.DiscountAmount.Should().Be(Money.Zero(Currencies.Default));
        order.AppliedCouponIds.Should().BeEmpty();
    }

    [Test]
    public void ApplyCoupon_WithMinOrderAmountMet_ShouldSucceed()
    {
        // Arrange
        var order = CreateValidOrder();
        var couponId = CouponId.CreateUnique();
        var minOrderAmount = new Money(10.00m, Currencies.Default); // Lower than order subtotal
        var couponValue = CouponValue.CreatePercentage(10m).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;

        // Act
        var result = order.ApplyCoupon(couponId, couponValue, appliesTo, minOrderAmount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.AppliedCouponIds.Should().Contain(couponId);
    }

    [Test]
    public void ApplyCoupon_WhenCouponAlreadyApplied_ShouldFailWithAlreadyAppliedError()
    {
        // Arrange
        var order = CreateValidOrder();
        var firstCouponId = CouponId.CreateUnique();
        var secondCouponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreatePercentage(10m).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;
        
        // Apply first coupon
        order.ApplyCoupon(firstCouponId, couponValue, appliesTo, null);

        // Act - Try to apply second coupon
        var result = order.ApplyCoupon(secondCouponId, couponValue, appliesTo, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.CouponAlreadyApplied);
        order.AppliedCouponIds.Should().ContainSingle();
        order.AppliedCouponIds.Should().Contain(firstCouponId);
    }

    [Test]
    public void ApplyCoupon_WithOrderNotInPlacedStatus_ShouldFailWithInvalidStatusError()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Accept(DateTime.UtcNow.AddHours(1)); // Change status to Accepted
        
        var couponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreatePercentage(10m).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;

        // Act
        var result = order.ApplyCoupon(couponId, couponValue, appliesTo, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrderErrors.CouponCannotBeAppliedToOrderStatus);
        order.DiscountAmount.Should().Be(Money.Zero(Currencies.Default));
        order.AppliedCouponIds.Should().BeEmpty();
    }

    [Test]
    public void ApplyCoupon_WithMultipleOrderItems_ShouldCalculateCorrectDiscount()
    {
        // Arrange
        var orderItems = new List<OrderItem>
        {
            OrderItem.Create(MenuCategoryId.CreateUnique(), MenuItemId.CreateUnique(), "Item 1", new Money(10.00m, Currencies.Default), 1).Value,
            OrderItem.Create(MenuCategoryId.CreateUnique(), MenuItemId.CreateUnique(), "Item 2", new Money(15.00m, Currencies.Default), 2).Value
        };
        
        var order = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            orderItems,
            DefaultSpecialInstructions,
            DefaultDiscountAmount,
            DefaultDeliveryFee,
            DefaultTipAmount,
            DefaultTaxAmount).Value;

        var couponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreatePercentage(10m).Value; // 10% discount
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;
        
        // Subtotal = 10 + (15 * 2) = 40
        var expectedDiscount = new Money(40.00m * 0.10m, Currencies.Default); // 4.00

        // Act
        var result = order.ApplyCoupon(couponId, couponValue, appliesTo, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(expectedDiscount);
    }

    [Test]
    public void ApplyCoupon_WithFreeItemCoupon_MultipleMatchingItems_ShouldApplyToCheapestUnit()
    {
        // Arrange - Create order with multiple items of the same type but different customization costs
        var freeItemId = MenuItemId.CreateUnique();
        var categoryId = MenuCategoryId.CreateUnique();
        
        var orderItems = new List<OrderItem>
        {
            // Same item, different quantities and customizations (different per-unit costs)
            OrderItem.Create(categoryId, freeItemId, "Pizza", new Money(10.00m, Currencies.Default), 1).Value, // $10 per unit
            OrderItem.Create(categoryId, freeItemId, "Pizza", new Money(24.00m, Currencies.Default), 2).Value  // $12 per unit
        };
        
        var order = Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            orderItems,
            DefaultSpecialInstructions,
            DefaultDiscountAmount,
            DefaultDeliveryFee,
            DefaultTipAmount,
            DefaultTaxAmount).Value;

        var couponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreateFreeItem(freeItemId).Value;
        var appliesTo = AppliesTo.CreateForSpecificItems(new List<MenuItemId> { freeItemId }).Value;
        
        // Expected discount should be the cheapest per-unit price: $10 (not $12)
        var expectedDiscount = new Money(10.00m, Currencies.Default);

        // Act
        var result = order.ApplyCoupon(couponId, couponValue, appliesTo, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(expectedDiscount);
        order.AppliedCouponIds.Should().Contain(couponId);
    }

    #endregion

    #region RemoveCoupon() Method Tests

    [Test]
    public void RemoveCoupon_WithAppliedCoupon_ShouldSucceedAndRemoveDiscount()
    {
        // Arrange
        var order = CreateValidOrder();
        var couponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreatePercentage(10m).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;
        
        order.ApplyCoupon(couponId, couponValue, appliesTo, null);

        // Act
        var result = order.RemoveCoupon();

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountAmount.Should().Be(Money.Zero(Currencies.Default));
        order.AppliedCouponIds.Should().BeEmpty();
        order.LastUpdateTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        // Verify total amount is recalculated without discount
        var expectedTotal = new Money(order.Subtotal.Amount + 
                                    order.TaxAmount.Amount + order.DeliveryFee.Amount + order.TipAmount.Amount, Currencies.Default);
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
            new Money(10.00m, Currencies.Default),
            2).Value;

        return new List<OrderItem> { orderItem };
    }

    #endregion
}
