using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.Services;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.Events;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.Services;

[TestFixture]
public class TeamCartConversionServiceTests
{
    private TeamCartConversionService _conversionService = null!;
    private UserId _hostUserId = null!;
    private UserId _guest1UserId = null!;
    private UserId _guest2UserId = null!;
    private RestaurantId _restaurantId = null!;
    private DeliveryAddress _deliveryAddress = null!;
    private const string _specialInstructions = "Please ring the bell";

    [SetUp]
    public void SetUp()
    {
        _conversionService = new TeamCartConversionService();
        _hostUserId = UserId.CreateUnique();
        _guest1UserId = UserId.CreateUnique();
        _guest2UserId = UserId.CreateUnique();
        _restaurantId = RestaurantId.CreateUnique();
        _deliveryAddress = DeliveryAddress.Create(
            "123 Test Street",
            "Test City",
            "Test State",
            "12345",
            "Test Country").Value;
    }

    #region ConvertToOrder() Success Scenarios

    [Test]
    public void ConvertToOrder_WithValidTeamCartAndMinimalItems_ShouldSucceed()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyToConfirm();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (order, updatedTeamCart) = result.Value;
        
        order.Should().NotBeNull();
        order.CustomerId.Should().Be(_hostUserId);
        order.RestaurantId.Should().Be(_restaurantId);
        order.DeliveryAddress.Should().Be(_deliveryAddress);
        order.SpecialInstructions.Should().Be(_specialInstructions);
        order.SourceTeamCartId.Should().Be(teamCart.Id);
        order.OrderItems.Should().HaveCount(2);
        order.PaymentTransactions.Should().HaveCount(2); // One online, one COD
        
        updatedTeamCart.Status.Should().Be(TeamCartStatus.Converted);
        updatedTeamCart.DomainEvents.Should().ContainSingle(e => e is TeamCartConverted);
    }

    [Test]
    public void ConvertToOrder_WithCustomizedItems_ShouldSucceed()
    {
        // Arrange
        var teamCart = CreateTeamCartWithCustomizedItems();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;
        
        order.OrderItems.Should().HaveCount(2);
        order.OrderItems.Should().OnlyContain(item => item.SelectedCustomizations.Any());
        
        // Verify customizations are properly mapped
        var firstItem = order.OrderItems.First();
        firstItem.SelectedCustomizations.Should().HaveCount(1);
        firstItem.SelectedCustomizations.First().Snapshot_CustomizationGroupName.Should().Be("Size");
        firstItem.SelectedCustomizations.First().Snapshot_ChoiceName.Should().Be("Large");
    }

    [Test]
    public void ConvertToOrder_WithMixedPaymentMethods_ShouldSucceed()
    {
        // Arrange
        var teamCart = CreateTeamCartWithMixedPayments();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;
        
        order.PaymentTransactions.Should().HaveCount(2);
        
        // Should have one online payment transaction
        var onlineTransaction = order.PaymentTransactions.Single(pt => pt.PaymentMethodType == PaymentMethodType.CreditCard);
        onlineTransaction.PaidByUserId.Should().Be(_guest1UserId);
        onlineTransaction.Amount.Amount.Should().Be(15.50m);
        
        // Should have one COD transaction for host
        var codTransaction = order.PaymentTransactions.Single(pt => pt.PaymentMethodType == PaymentMethodType.CashOnDelivery);
        codTransaction.PaidByUserId.Should().Be(_hostUserId);
        codTransaction.Amount.Amount.Should().Be(20.00m);
    }

    [Test]
    public void ConvertToOrder_WithOnlinePaymentsOnly_ShouldSucceed()
    {
        // Arrange
        var teamCart = CreateTeamCartWithOnlinePaymentsOnly();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;
        
        order.PaymentTransactions.Should().HaveCount(2);
        order.PaymentTransactions.Should().OnlyContain(pt => pt.PaymentMethodType == PaymentMethodType.CreditCard);
        
        // Each member should have their own transaction
        var hostTransactions = order.PaymentTransactions.Where(pt => pt.PaidByUserId! == _hostUserId);
        hostTransactions.Should().HaveCount(1);
        
        var guestTransactions = order.PaymentTransactions.Where(pt => pt.PaidByUserId! == _guest1UserId);
        guestTransactions.Should().HaveCount(1);
    }

    [Test]
    public void ConvertToOrder_WithCODPaymentsOnly_ShouldSucceed()
    {
        // Arrange
        var teamCart = CreateTeamCartWithCODPaymentsOnly();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;
        
        order.PaymentTransactions.Should().HaveCount(1);
        
        var codTransaction = order.PaymentTransactions.Single();
        codTransaction.PaymentMethodType.Should().Be(PaymentMethodType.CashOnDelivery);
        codTransaction.PaidByUserId.Should().Be(_hostUserId); // Host is guarantor
        codTransaction.Amount.Amount.Should().Be(35.50m); // Total of both members
    }

    [Test]
    public void ConvertToOrder_WithEmptySpecialInstructions_ShouldSucceed()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyToConfirm();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, string.Empty);

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;
        
        order.SpecialInstructions.Should().Be(string.Empty);
    }

    #endregion

    #region ConvertToOrder() Validation Tests

    [Test]
    public void ConvertToOrder_WithInvalidTeamCartStatus_ShouldFailWithInvalidStatusError()
    {
        // Arrange
        var teamCart = CreateTeamCartInAwaitingPaymentsStatus();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidStatusForConversion);
    }

    [Test]
    public void ConvertToOrder_WithNoItems_ShouldFailWithConversionDataIncompleteError()
    {
        // Arrange
        var teamCart = CreateTeamCartWithoutItems();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.ConversionDataIncomplete);
    }

    [Test]
    public void ConvertToOrder_WithNoPayments_ShouldFailWithCannotConvertWithoutPaymentsError()
    {
        // Arrange
        var teamCart = CreateTeamCartWithoutPayments();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CannotConvertWithoutPayments);
    }

    #endregion

    #region PaymentTransaction Mapping Tests

    [Test]
    public void ConvertToOrder_WithOnlinePayments_ShouldCreateIndividualTransactions()
    {
        // Arrange
        var teamCart = CreateTeamCartWithOnlinePaymentsOnly();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;
        
        order.PaymentTransactions.Should().HaveCount(2);
        
        // Each member should have their own transaction
        var hostTransaction = order.PaymentTransactions.Single(pt => pt.PaidByUserId?.Equals(_hostUserId) == true);
        hostTransaction.Amount.Amount.Should().Be(20.00m);
        hostTransaction.PaymentMethodType.Should().Be(PaymentMethodType.CreditCard);
        hostTransaction.PaymentMethodDisplay.Should().Be("Online Payment");
        
        var guestTransaction = order.PaymentTransactions.Single(pt => pt.PaidByUserId?.Equals(_guest1UserId) == true);
        guestTransaction.Amount.Amount.Should().Be(15.50m);
        guestTransaction.PaymentMethodType.Should().Be(PaymentMethodType.CreditCard);
        guestTransaction.PaymentMethodDisplay.Should().Be("Online Payment");
    }

    [Test]
    public void ConvertToOrder_WithCODPayments_ShouldCreateSingleTransactionForHost()
    {
        // Arrange
        var teamCart = CreateTeamCartWithCODPaymentsOnly();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;
        
        order.PaymentTransactions.Should().HaveCount(1);
        
        var codTransaction = order.PaymentTransactions.Single();
        codTransaction.PaymentMethodType.Should().Be(PaymentMethodType.CashOnDelivery);
        codTransaction.PaidByUserId.Should().Be(_hostUserId);
        codTransaction.Amount.Amount.Should().Be(35.50m); // Total of both members
        codTransaction.PaymentMethodDisplay.Should().Be("Cash on Delivery");
        codTransaction.PaymentGatewayReferenceId.Should().BeNull();
    }

    [Test]
    public void ConvertToOrder_ShouldSetCorrectTransactionTimestamps()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyToConfirm();
        var beforeConversion = DateTime.UtcNow;

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;
        
        var afterConversion = DateTime.UtcNow;
        
        order.PaymentTransactions.Should().OnlyContain(pt => 
            pt.Timestamp >= beforeConversion && pt.Timestamp <= afterConversion);
    }

    #endregion

    #region Financial Details Tests

    [Test]
    public void ConvertToOrder_WithTipAndDiscount_ShouldCreateOrderWithCorrectFinancials()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyToConfirm();
        
        // Apply tip and coupon
        var tipAmount = new Money(3.50m, Currencies.Default);
        var couponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreateFixedAmount(new Money(5.00m, Currencies.Default)).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;
        
        // Apply tip and coupon directly
        teamCart.ApplyTip(_hostUserId, tipAmount);
        teamCart.ApplyCoupon(_hostUserId, couponId, couponValue, appliesTo, null);

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;
        
        order.TipAmount.Should().Be(tipAmount);
        order.DiscountAmount.Amount.Should().Be(5.00m);
    }

    [Test]
    public void ConvertToOrder_WithCoupon_ShouldTransferCouponToOrder()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyToConfirm();
        
        // Apply coupon
        var couponId = CouponId.CreateUnique();
        var couponValue = CouponValue.CreateFixedAmount(new Money(5.00m, Currencies.Default)).Value;
        var appliesTo = AppliesTo.CreateForWholeOrder().Value;
        
        // Use reflection to access private methods for testing
        var applyCouponMethod = typeof(TeamCart).GetMethod("ApplyCoupon", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        applyCouponMethod?.Invoke(teamCart, [_hostUserId, couponId, couponValue, appliesTo, null]);

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;
        
        order.AppliedCouponId.Should().Be(couponId);
    }

#endregion

#region OrderItem Mapping Tests

    [Test]
    public void ConvertToOrder_ShouldMapItemsCorrectly()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyToConfirm();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;
        
        order.OrderItems.Should().HaveCount(2);
        
        // Verify mapping of first item
        var firstItem = order.OrderItems.First();
        firstItem.Snapshot_ItemName.Should().Be("Test Item 1");
        firstItem.Snapshot_BasePriceAtOrder.Amount.Should().Be(20.00m);
        firstItem.Quantity.Should().Be(1);
        
        // Verify mapping of second item
        var secondItem = order.OrderItems.Skip(1).First();
        secondItem.Snapshot_ItemName.Should().Be("Test Item 2");
        secondItem.Snapshot_BasePriceAtOrder.Amount.Should().Be(15.50m);
        secondItem.Quantity.Should().Be(1);
    }

    [Test]
    public void ConvertToOrder_ShouldMapCustomizationsCorrectly()
    {
        // Arrange
        var teamCart = CreateTeamCartWithCustomizedItems();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;
        
        var itemWithCustomizations = order.OrderItems.First();
        itemWithCustomizations.SelectedCustomizations.Should().HaveCount(1);
        
        var customization = itemWithCustomizations.SelectedCustomizations.First();
        customization.Snapshot_CustomizationGroupName.Should().Be("Size");
        customization.Snapshot_ChoiceName.Should().Be("Large");
        customization.Snapshot_ChoicePriceAdjustmentAtOrder.Amount.Should().Be(2.00m);
    }

    #endregion

    #region Order Property Tests

    [Test]
    public void ConvertToOrder_ShouldSetHostAsCustomer()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyToConfirm();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;
        
        order.CustomerId.Should().Be(_hostUserId);
    }

    [Test]
    public void ConvertToOrder_ShouldSetCorrectRestaurantId()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyToConfirm();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;
        
        order.RestaurantId.Should().Be(_restaurantId);
    }

    [Test]
    public void ConvertToOrder_ShouldSetSourceTeamCartId()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyToConfirm();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;
        
        order.SourceTeamCartId.Should().Be(teamCart.Id);
    }

    [Test]
    public void ConvertToOrder_ShouldSetCorrectDeliveryAddress()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyToConfirm();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;
        
        order.DeliveryAddress.Should().Be(_deliveryAddress);
    }

    #endregion

    #region Domain Event Tests

    [Test]
    public void ConvertToOrder_ShouldRaiseTeamCartConvertedEvent()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyToConfirm();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (order, updatedTeamCart) = result.Value;
        
        updatedTeamCart.DomainEvents.Should().ContainSingle(e => e is TeamCartConverted);
        
        var convertedEvent = updatedTeamCart.DomainEvents.OfType<TeamCartConverted>().Single();
        convertedEvent.TeamCartId.Should().Be(teamCart.Id);
        convertedEvent.OrderId.Should().Be(order.Id);
        convertedEvent.ConvertedByUserId.Should().Be(_hostUserId);
        convertedEvent.ConvertedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void ConvertToOrder_ShouldUpdateTeamCartStatusToConverted()
    {
        // Arrange
        var teamCart = CreateTeamCartReadyToConfirm();

        // Act
        var result = _conversionService.ConvertToOrder(teamCart, _deliveryAddress, _specialInstructions);

        // Assert
        result.ShouldBeSuccessful();
        var (_, updatedTeamCart) = result.Value;
        
        updatedTeamCart.Status.Should().Be(TeamCartStatus.Converted);
    }

    #endregion

    #region Helper Methods

    private TeamCart CreateTeamCartReadyToConfirm()
    {
        var teamCart = TeamCart.Create(_hostUserId, _restaurantId, "Host User").Value;
        teamCart.AddMember(_guest1UserId, "Guest 1");
        
        // Add items
        AddItemToTeamCart(teamCart, _hostUserId, "Test Item 1", 20.00m);
        AddItemToTeamCart(teamCart, _guest1UserId, "Test Item 2", 15.50m);
        
        // Initiate checkout
        teamCart.InitiateCheckout(_hostUserId);
        
        // Commit payments
        teamCart.CommitToCashOnDelivery(_hostUserId, new Money(20.00m, Currencies.Default));
        teamCart.RecordSuccessfulOnlinePayment(_guest1UserId, new Money(15.50m, Currencies.Default), "txn_123");
        
        return teamCart;
    }

    private TeamCart CreateTeamCartWithCustomizedItems()
    {
        var teamCart = TeamCart.Create(_hostUserId, _restaurantId, "Host User").Value;
        teamCart.AddMember(_guest1UserId, "Guest 1");
        
        // Add items with customizations
        var customization = TeamCartItemCustomization.Create(
            "Size", "Large", new Money(2.00m, Currencies.Default)).Value;
        
        AddItemToTeamCart(teamCart, _hostUserId, "Test Item 1", 20.00m, [customization]);
        AddItemToTeamCart(teamCart, _guest1UserId, "Test Item 2", 15.50m, [customization]);
        
        // Complete payment flow
        teamCart.InitiateCheckout(_hostUserId);
        teamCart.CommitToCashOnDelivery(_hostUserId, new Money(22.00m, Currencies.Default));
        teamCart.RecordSuccessfulOnlinePayment(_guest1UserId, new Money(17.50m, Currencies.Default), "txn_123");
        
        return teamCart;
    }

    private TeamCart CreateTeamCartWithMixedPayments()
    {
        return CreateTeamCartReadyToConfirm(); // This already has mixed payments
    }

    private TeamCart CreateTeamCartWithOnlinePaymentsOnly()
    {
        var teamCart = TeamCart.Create(_hostUserId, _restaurantId, "Host User").Value;
        teamCart.AddMember(_guest1UserId, "Guest 1");
        
        // Add items
        AddItemToTeamCart(teamCart, _hostUserId, "Test Item 1", 20.00m);
        AddItemToTeamCart(teamCart, _guest1UserId, "Test Item 2", 15.50m);
        
        // Initiate checkout
        teamCart.InitiateCheckout(_hostUserId);
        
        // Both pay online
        teamCart.RecordSuccessfulOnlinePayment(_hostUserId, new Money(20.00m, Currencies.Default), "txn_456");
        teamCart.RecordSuccessfulOnlinePayment(_guest1UserId, new Money(15.50m, Currencies.Default), "txn_123");
        
        return teamCart;
    }

    private TeamCart CreateTeamCartWithCODPaymentsOnly()
    {
        var teamCart = TeamCart.Create(_hostUserId, _restaurantId, "Host User").Value;
        teamCart.AddMember(_guest1UserId, "Guest 1");
        
        // Add items
        AddItemToTeamCart(teamCart, _hostUserId, "Test Item 1", 20.00m);
        AddItemToTeamCart(teamCart, _guest1UserId, "Test Item 2", 15.50m);
        
        // Initiate checkout
        teamCart.InitiateCheckout(_hostUserId);
        
        // Both pay COD
        teamCart.CommitToCashOnDelivery(_hostUserId, new Money(20.00m, Currencies.Default));
        teamCart.CommitToCashOnDelivery(_guest1UserId, new Money(15.50m, Currencies.Default));
        
        return teamCart;
    }

    private TeamCart CreateTeamCartInAwaitingPaymentsStatus()
    {
        var teamCart = TeamCart.Create(_hostUserId, _restaurantId, "Host User").Value;
        teamCart.AddMember(_guest1UserId, "Guest 1");
        
        // Add items
        AddItemToTeamCart(teamCart, _hostUserId, "Test Item 1", 20.00m);
        AddItemToTeamCart(teamCart, _guest1UserId, "Test Item 2", 15.50m);
        
        // Initiate checkout but don't complete payments
        teamCart.InitiateCheckout(_hostUserId);
        
        return teamCart;
    }

    private TeamCart CreateTeamCartWithoutItems()
    {
        var teamCart = TeamCart.Create(_hostUserId, _restaurantId, "Host User").Value;
        teamCart.AddMember(_guest1UserId, "Guest 1");
        
        // Set status to ReadyToConfirm without items (using reflection for testing)
        typeof(TeamCart).GetProperty("Status")?.SetValue(teamCart, TeamCartStatus.ReadyToConfirm);
        
        return teamCart;
    }

    private TeamCart CreateTeamCartWithoutPayments()
    {
        var teamCart = TeamCart.Create(_hostUserId, _restaurantId, "Host User").Value;
        teamCart.AddMember(_guest1UserId, "Guest 1");
        
        // Add items
        AddItemToTeamCart(teamCart, _hostUserId, "Test Item 1", 20.00m);
        AddItemToTeamCart(teamCart, _guest1UserId, "Test Item 2", 15.50m);
        
        // Set status to ReadyToConfirm without payments (using reflection for testing)
        typeof(TeamCart).GetProperty("Status")?.SetValue(teamCart, TeamCartStatus.ReadyToConfirm);
        
        return teamCart;
    }

    private void AddItemToTeamCart(
        TeamCart teamCart, 
        UserId userId, 
        string itemName, 
        decimal price, 
        List<TeamCartItemCustomization>? customizations = null)
    {
        teamCart.AddItem(
            userId,
            MenuItemId.CreateUnique(),
            MenuCategoryId.CreateUnique(),
            itemName,
            new Money(price, Currencies.Default),
            1,
            customizations);
    }

    #endregion
}
