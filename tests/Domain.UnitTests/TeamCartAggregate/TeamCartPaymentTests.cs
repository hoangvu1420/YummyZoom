using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.Events;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using static YummyZoom.Domain.UnitTests.TeamCartAggregate.TeamCartTestHelpers;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate;

[TestFixture]
public class TeamCartPaymentTests
{
    private TeamCart _teamCart = null!;
    private MenuItemId _menuItemId1 = null!;
    private MenuItemId _menuItemId2 = null!;
    private MenuCategoryId _menuCategoryId = null!;
    private Money _hostItemAmount = null!;
    private Money _guest1ItemAmount = null!;
    private Money _guest2ItemAmount = null!;

    [SetUp]
    public void SetUp()
    {
        _menuItemId1 = MenuItemId.CreateUnique();
        _menuItemId2 = MenuItemId.CreateUnique();
        _menuCategoryId = MenuCategoryId.CreateUnique();
        _hostItemAmount = new Money(10.00m, Currencies.Default);
        _guest1ItemAmount = new Money(15.00m, Currencies.Default);
        _guest2ItemAmount = new Money(20.00m, Currencies.Default);

        // Create a team cart and add members
        _teamCart = TeamCart.Create(DefaultHostUserId, DefaultRestaurantId, DefaultHostName).Value;
        _teamCart.AddMember(DefaultGuestUserId1, "Guest User 1").ShouldBeSuccessful();
        _teamCart.AddMember(DefaultGuestUserId2, "Guest User 2").ShouldBeSuccessful();

        // Add items to the cart
        _teamCart.AddItem(
            DefaultHostUserId,
            _menuItemId1,
            _menuCategoryId,
            "Host Item",
            _hostItemAmount,
            1).ShouldBeSuccessful();

        _teamCart.AddItem(
            DefaultGuestUserId1,
            _menuItemId2,
            _menuCategoryId,
            "Guest 1 Item",
            _guest1ItemAmount,
            1).ShouldBeSuccessful();
            
        _teamCart.AddItem(
            DefaultGuestUserId2,
            _menuItemId1,
            _menuCategoryId,
            "Guest 2 Item",
            _guest2ItemAmount,
            1).ShouldBeSuccessful();

        // Lock the cart for payment
        _teamCart.LockForPayment(DefaultHostUserId).ShouldBeSuccessful();
    }

    #region LockForPayment Tests

    [Test]
    public void LockForPayment_ByHost_ShouldSucceedAndTransitionToLocked()
    {
        // Arrange
        var teamCart = CreateValidTeamCart();
        teamCart.AddMember(UserId.CreateUnique(), "Guest User").ShouldBeSuccessful();
        teamCart.AddItem(DefaultHostUserId, MenuItemId.CreateUnique(), MenuCategoryId.CreateUnique(), "Item", new Money(10m, Currencies.Default), 1).ShouldBeSuccessful();
        teamCart.ClearDomainEvents(); // Clear events from creation and adding items

        // Act
        var result = teamCart.LockForPayment(DefaultHostUserId);

        // Assert
        result.ShouldBeSuccessful();
        teamCart.Status.Should().Be(TeamCartStatus.Locked);
        teamCart.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(TeamCartLockedForPayment));
        var lockedEvent = teamCart.DomainEvents.OfType<TeamCartLockedForPayment>().Single();
        lockedEvent.TeamCartId.Should().Be(teamCart.Id);
        lockedEvent.HostUserId.Should().Be(DefaultHostUserId);
    }

    [Test]
    public void LockForPayment_ByGuest_ShouldFailWithOnlyHostCanLockCartError()
    {
        // Arrange
        var teamCart = CreateTeamCartWithGuest();
        teamCart.AddItem(DefaultHostUserId, MenuItemId.CreateUnique(), MenuCategoryId.CreateUnique(), "Item", new Money(10m, Currencies.Default), 1).ShouldBeSuccessful();
        teamCart.ClearDomainEvents();

        // Act
        var result = teamCart.LockForPayment(teamCart.Members.First(m => m.UserId != DefaultHostUserId).UserId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.OnlyHostCanLockCart);
        teamCart.Status.Should().Be(TeamCartStatus.Open);
        teamCart.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void LockForPayment_WhenCartHasNoItems_ShouldFailWithCannotLockEmptyCartError()
    {
        // Arrange
        var teamCart = CreateValidTeamCart(); // No items added
        teamCart.AddMember(UserId.CreateUnique(), "Guest User").ShouldBeSuccessful();
        teamCart.ClearDomainEvents();

        // Act
        var result = teamCart.LockForPayment(DefaultHostUserId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CannotLockEmptyCart);
        teamCart.Status.Should().Be(TeamCartStatus.Open);
        teamCart.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void LockForPayment_WhenCartIsNotOpen_ShouldFailWithCannotLockCartInCurrentStatus()
    {
        // Arrange - Test when cart is already Locked
        var teamCartLocked = CreateValidTeamCart();
        teamCartLocked.AddMember(UserId.CreateUnique(), "Guest User").ShouldBeSuccessful();
        teamCartLocked.AddItem(DefaultHostUserId, MenuItemId.CreateUnique(), MenuCategoryId.CreateUnique(), "Item", new Money(10m, Currencies.Default), 1).ShouldBeSuccessful();
        teamCartLocked.LockForPayment(DefaultHostUserId).ShouldBeSuccessful();
        teamCartLocked.ClearDomainEvents();
        var resultLocked = teamCartLocked.LockForPayment(DefaultHostUserId);
        resultLocked.ShouldBeFailure();
        resultLocked.Error.Should().Be(TeamCartErrors.CannotLockCartInCurrentStatus);
        teamCartLocked.Status.Should().Be(TeamCartStatus.Locked);

        // Arrange - Test when cart is ReadyToConfirm
        var teamCartReady = CreateTeamCartReadyForConversion();
        teamCartReady.ClearDomainEvents();
        var resultReady = teamCartReady.LockForPayment(DefaultHostUserId);
        resultReady.ShouldBeFailure();
        resultReady.Error.Should().Be(TeamCartErrors.CannotLockCartInCurrentStatus);
        teamCartReady.Status.Should().Be(TeamCartStatus.ReadyToConfirm);

        // Arrange - Test when cart is Converted
        var teamCartConverted = CreateConvertedTeamCart();
        teamCartConverted.ClearDomainEvents();
        var resultConverted = teamCartConverted.LockForPayment(DefaultHostUserId);
        resultConverted.ShouldBeFailure();
        resultConverted.Error.Should().Be(TeamCartErrors.CannotLockCartInCurrentStatus);
        teamCartConverted.Status.Should().Be(TeamCartStatus.Converted);

        // Arrange - Test when cart is Expired
        var teamCartExpired = CreateExpiredTeamCart();
        teamCartExpired.ClearDomainEvents();
        var resultExpired = teamCartExpired.LockForPayment(DefaultHostUserId);
        resultExpired.ShouldBeFailure();
        resultExpired.Error.Should().Be(TeamCartErrors.CannotLockCartInCurrentStatus);
        teamCartExpired.Status.Should().Be(TeamCartStatus.Expired);
    }

    #endregion

    #region CommitToCashOnDelivery Tests

    [Test]
    public void CommitToCashOnDelivery_WithValidData_ShouldSucceed()
    {
        // Act
        var result = _teamCart.CommitToCashOnDelivery(DefaultHostUserId, _hostItemAmount);

        // Assert
        result.ShouldBeSuccessful();
        var payment = _teamCart.MemberPayments.FirstOrDefault(p => p.UserId == DefaultHostUserId);
        payment.Should().NotBeNull();
        payment!.Method.Should().Be(PaymentMethod.CashOnDelivery);
        payment.Amount.Should().Be(_hostItemAmount);
        payment.Status.Should().Be(PaymentStatus.CommittedToCOD);
    }

    [Test]
    public void CommitToCashOnDelivery_WithInvalidAmount_ShouldFail()
    {
        // Arrange
        var invalidAmount = new Money(50.00m, Currencies.Default);

        // Act
        var result = _teamCart.CommitToCashOnDelivery(DefaultHostUserId, invalidAmount);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidPaymentAmount);
        _teamCart.MemberPayments.Should().NotContain(p => p.UserId == DefaultHostUserId);
    }

    [Test]
    public void CommitToCashOnDelivery_WhenUserNotMember_ShouldFail()
    {
        // Arrange
        var nonMemberUserId = UserId.CreateUnique();

        // Act
        var result = _teamCart.CommitToCashOnDelivery(nonMemberUserId, _hostItemAmount);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.UserNotMember);
        _teamCart.MemberPayments.Should().NotContain(p => p.UserId == nonMemberUserId);
    }

    [Test]
    public void CommitToCashOnDelivery_WhenCartIsNotLocked_ShouldFail()
    {
        // Arrange
        var newCart = CreateValidTeamCart(); // Status is Open
        newCart.AddMember(DefaultGuestUserId1, "Guest User").ShouldBeSuccessful(); // Ensure guest is member
        AddItemToCart(newCart, DefaultHostUserId, 10.00m); // Add item for host
        AddItemToCart(newCart, DefaultGuestUserId1, 15.00m); // Add item for guest
        newCart.ClearDomainEvents();

        // Act
        var result = newCart.CommitToCashOnDelivery(DefaultHostUserId, new Money(10.00m, Currencies.Default));

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CanOnlyPayOnLockedCart);
        newCart.MemberPayments.Should().BeEmpty();
    }

    [Test]
    public void CommitToCashOnDelivery_ShouldReplaceExistingPayment()
    {
        // Arrange - First record a successful online payment
        _teamCart.RecordSuccessfulOnlinePayment(DefaultHostUserId, _hostItemAmount, "txn_123").ShouldBeSuccessful();
        var initialPaymentCount = _teamCart.MemberPayments.Count;

        // Act - Then switch to COD
        var result = _teamCart.CommitToCashOnDelivery(DefaultHostUserId, _hostItemAmount);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.MemberPayments.Count.Should().Be(initialPaymentCount); // Count should remain the same
        var payment = _teamCart.MemberPayments.FirstOrDefault(p => p.UserId == DefaultHostUserId);
        payment.Should().NotBeNull();
        payment!.Method.Should().Be(PaymentMethod.CashOnDelivery); // Method should be updated
    }

    [Test]
    public void CommitToCashOnDelivery_AllMembersCommitted_ShouldTransitionToReadyToConfirm()
    {
        // Arrange - Other members commit to payment
        _teamCart.RecordSuccessfulOnlinePayment(DefaultGuestUserId1, _guest1ItemAmount, "txn_123").ShouldBeSuccessful();
        _teamCart.CommitToCashOnDelivery(DefaultGuestUserId2, _guest2ItemAmount).ShouldBeSuccessful();

        // Act - Last member commits
        var result = _teamCart.CommitToCashOnDelivery(DefaultHostUserId, _hostItemAmount);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.Status.Should().Be(TeamCartStatus.ReadyToConfirm);
    }

    [Test]
    public void CommitToCashOnDelivery_RaisesCorrectDomainEvent()
    {
        // Act
        _teamCart.CommitToCashOnDelivery(DefaultHostUserId, _hostItemAmount).ShouldBeSuccessful();

        // Assert
        var paymentEvent = _teamCart.DomainEvents
            .OfType<MemberCommittedToPayment>()
            .FirstOrDefault();
        paymentEvent.Should().NotBeNull();
        paymentEvent!.UserId.Should().Be(DefaultHostUserId);
        paymentEvent.Method.Should().Be(PaymentMethod.CashOnDelivery);
        paymentEvent.Amount.Should().Be(_hostItemAmount);
    }

    #endregion

    #region RecordSuccessfulOnlinePayment Tests

    [Test]
    public void RecordSuccessfulOnlinePayment_WithValidData_ShouldSucceed()
    {
        // Act
        var result = _teamCart.RecordSuccessfulOnlinePayment(DefaultHostUserId, _hostItemAmount, "txn_123");

        // Assert
        result.ShouldBeSuccessful();
        var payment = _teamCart.MemberPayments.FirstOrDefault(p => p.UserId == DefaultHostUserId);
        payment.Should().NotBeNull();
        payment!.Method.Should().Be(PaymentMethod.Online);
        payment.Amount.Should().Be(_hostItemAmount);
        payment.Status.Should().Be(PaymentStatus.PaidOnline);
    }

    [Test]
    public void RecordSuccessfulOnlinePayment_WithInvalidAmount_ShouldFail()
    {
        // Arrange
        var invalidAmount = new Money(50.00m, Currencies.Default);

        // Act
        var result = _teamCart.RecordSuccessfulOnlinePayment(DefaultHostUserId, invalidAmount, "txn_123");

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidPaymentAmount);
        _teamCart.MemberPayments.Should().NotContain(p => p.UserId == DefaultHostUserId);
    }

    [Test]
    public void RecordSuccessfulOnlinePayment_WithEmptyTransactionId_ShouldFail()
    {
        // Act
        var result = _teamCart.RecordSuccessfulOnlinePayment(DefaultHostUserId, _hostItemAmount, "");

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidTransactionId);
        _teamCart.MemberPayments.Should().NotContain(p => p.UserId == DefaultHostUserId);
    }

    [Test]
    public void RecordSuccessfulOnlinePayment_WhenUserNotMember_ShouldFail()
    {
        // Arrange
        var nonMemberUserId = UserId.CreateUnique();

        // Act
        var result = _teamCart.RecordSuccessfulOnlinePayment(nonMemberUserId, _hostItemAmount, "txn_123");

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.UserNotMember);
        _teamCart.MemberPayments.Should().NotContain(p => p.UserId == nonMemberUserId);
    }

    [Test]
    public void RecordSuccessfulOnlinePayment_WhenCartIsNotLocked_ShouldFail()
    {
        // Arrange
        var newCart = CreateValidTeamCart(); // Status is Open
        newCart.AddMember(DefaultGuestUserId1, "Guest User").ShouldBeSuccessful(); // Ensure guest is member
        AddItemToCart(newCart, DefaultHostUserId, 10.00m); // Add item for host
        AddItemToCart(newCart, DefaultGuestUserId1, 15.00m); // Add item for guest
        newCart.ClearDomainEvents();

        // Act
        var result = newCart.RecordSuccessfulOnlinePayment(DefaultHostUserId, new Money(10.00m, Currencies.Default), "txn_123");

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CanOnlyPayOnLockedCart);
        newCart.MemberPayments.Should().BeEmpty();
    }

    [Test]
    public void RecordSuccessfulOnlinePayment_ShouldReplaceExistingCODPayment()
    {
        // Arrange - First commit to COD
        _teamCart.CommitToCashOnDelivery(DefaultHostUserId, _hostItemAmount).ShouldBeSuccessful();
        var initialPaymentCount = _teamCart.MemberPayments.Count;

        // Act - Then switch to online payment
        var result = _teamCart.RecordSuccessfulOnlinePayment(DefaultHostUserId, _hostItemAmount, "txn_123");

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.MemberPayments.Count.Should().Be(initialPaymentCount); // Count should remain the same
        var payment = _teamCart.MemberPayments.FirstOrDefault(p => p.UserId == DefaultHostUserId);
        payment.Should().NotBeNull();
        payment!.Method.Should().Be(PaymentMethod.Online); // Method should be updated
        payment.Status.Should().Be(PaymentStatus.PaidOnline); // Status should be updated
    }

    [Test]
    public void RecordSuccessfulOnlinePayment_AllMembersCommitted_ShouldTransitionToReadyToConfirm()
    {
        // Arrange - Other members commit to payment
        _teamCart.CommitToCashOnDelivery(DefaultGuestUserId1, _guest1ItemAmount).ShouldBeSuccessful();
        _teamCart.CommitToCashOnDelivery(DefaultGuestUserId2, _guest2ItemAmount).ShouldBeSuccessful();

        // Act - Last member commits with online payment
        var result = _teamCart.RecordSuccessfulOnlinePayment(DefaultHostUserId, _hostItemAmount, "txn_123");

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.Status.Should().Be(TeamCartStatus.ReadyToConfirm);
    }

    [Test]
    public void RecordSuccessfulOnlinePayment_RaisesCorrectDomainEvent()
    {
        // Act
        _teamCart.RecordSuccessfulOnlinePayment(DefaultHostUserId, _hostItemAmount, "txn_123").ShouldBeSuccessful();

        // Assert
        var paymentEvent = _teamCart.DomainEvents
            .OfType<OnlinePaymentSucceeded>()
            .FirstOrDefault();
        paymentEvent.Should().NotBeNull();
        paymentEvent!.UserId.Should().Be(DefaultHostUserId);
        paymentEvent.TransactionId.Should().Be("txn_123");
        paymentEvent.Amount.Should().Be(_hostItemAmount);
    }

    #endregion

    #region Payment Workflow Tests

    [Test]
    public void PaymentWorkflow_AllOnlinePayments_ShouldTransitionToReadyToConfirm()
    {
        // Arrange - Ensure cart is locked before payments
        var teamCart = CreateValidTeamCart();
        teamCart.AddMember(DefaultGuestUserId1, "Guest 1").ShouldBeSuccessful();
        teamCart.AddMember(DefaultGuestUserId2, "Guest 2").ShouldBeSuccessful();
        AddItemToCart(teamCart, DefaultHostUserId, 10.00m);
        AddItemToCart(teamCart, DefaultGuestUserId1, 15.00m);
        AddItemToCart(teamCart, DefaultGuestUserId2, 20.00m);
        teamCart.LockForPayment(DefaultHostUserId).ShouldBeSuccessful();
        teamCart.ClearDomainEvents();

        // Act - Complete all online payments
        teamCart.RecordSuccessfulOnlinePayment(DefaultHostUserId, new Money(10.00m, Currencies.Default), "txn_host").ShouldBeSuccessful();
        teamCart.RecordSuccessfulOnlinePayment(DefaultGuestUserId1, new Money(15.00m, Currencies.Default), "txn_guest1").ShouldBeSuccessful();
        teamCart.RecordSuccessfulOnlinePayment(DefaultGuestUserId2, new Money(20.00m, Currencies.Default), "txn_guest2").ShouldBeSuccessful();

        // Assert
        teamCart.Status.Should().Be(TeamCartStatus.ReadyToConfirm);

        // Check domain event
        var domainEvent = teamCart.DomainEvents
            .OfType<TeamCartReadyForConfirmation>()
            .FirstOrDefault();
        domainEvent.Should().NotBeNull();
        domainEvent!.TotalAmount.Amount.Should().Be(45.00m); // Sum of all payments
        domainEvent.CashAmount.Amount.Should().Be(0m); // No cash payments
    }

    [Test]
    public void PaymentWorkflow_AllCODPayments_ShouldTransitionToReadyToConfirm()
    {
        // Arrange - Ensure cart is locked before payments
        var teamCart = CreateValidTeamCart();
        teamCart.AddMember(DefaultGuestUserId1, "Guest 1").ShouldBeSuccessful();
        teamCart.AddMember(DefaultGuestUserId2, "Guest 2").ShouldBeSuccessful();
        AddItemToCart(teamCart, DefaultHostUserId, 10.00m);
        AddItemToCart(teamCart, DefaultGuestUserId1, 15.00m);
        AddItemToCart(teamCart, DefaultGuestUserId2, 20.00m);
        teamCart.LockForPayment(DefaultHostUserId).ShouldBeSuccessful();
        teamCart.ClearDomainEvents();

        // Act - Commit all to COD
        teamCart.CommitToCashOnDelivery(DefaultHostUserId, new Money(10.00m, Currencies.Default)).ShouldBeSuccessful();
        teamCart.CommitToCashOnDelivery(DefaultGuestUserId1, new Money(15.00m, Currencies.Default)).ShouldBeSuccessful();
        teamCart.CommitToCashOnDelivery(DefaultGuestUserId2, new Money(20.00m, Currencies.Default)).ShouldBeSuccessful();

        // Assert
        teamCart.Status.Should().Be(TeamCartStatus.ReadyToConfirm);

        // Check domain event
        var domainEvent = teamCart.DomainEvents
            .OfType<TeamCartReadyForConfirmation>()
            .FirstOrDefault();
        domainEvent.Should().NotBeNull();
        domainEvent!.TotalAmount.Amount.Should().Be(45.00m); // Sum of all payments
        domainEvent.CashAmount.Amount.Should().Be(45.00m); // All cash payments
    }

    [Test]
    public void PaymentWorkflow_MixedPayments_ShouldTransitionToReadyToConfirm()
    {
        // Arrange - Ensure cart is locked before payments
        var teamCart = CreateValidTeamCart();
        teamCart.AddMember(DefaultGuestUserId1, "Guest 1").ShouldBeSuccessful();
        teamCart.AddMember(DefaultGuestUserId2, "Guest 2").ShouldBeSuccessful();
        AddItemToCart(teamCart, DefaultHostUserId, 10.00m);
        AddItemToCart(teamCart, DefaultGuestUserId1, 15.00m);
        AddItemToCart(teamCart, DefaultGuestUserId2, 20.00m);
        teamCart.LockForPayment(DefaultHostUserId).ShouldBeSuccessful();
        teamCart.ClearDomainEvents();

        // Act - One online, two COD
        teamCart.RecordSuccessfulOnlinePayment(DefaultHostUserId, new Money(10.00m, Currencies.Default), "txn_host").ShouldBeSuccessful();
        teamCart.CommitToCashOnDelivery(DefaultGuestUserId1, new Money(15.00m, Currencies.Default)).ShouldBeSuccessful();
        teamCart.CommitToCashOnDelivery(DefaultGuestUserId2, new Money(20.00m, Currencies.Default)).ShouldBeSuccessful();

        // Assert
        teamCart.Status.Should().Be(TeamCartStatus.ReadyToConfirm);

        // Check domain event
        var domainEvent = teamCart.DomainEvents
            .OfType<TeamCartReadyForConfirmation>()
            .FirstOrDefault();
        domainEvent.Should().NotBeNull();
        domainEvent!.TotalAmount.Amount.Should().Be(45.00m); // Sum of all payments
        domainEvent.CashAmount.Amount.Should().Be(35.00m); // Sum of COD payments
    }

    #endregion

    #region Helper Methods

    private TeamCart CreateCartWithItems()
    {
        var cart = TeamCart.Create(DefaultHostUserId, DefaultRestaurantId, DefaultHostName).Value;
        cart.AddMember(DefaultGuestUserId1, "Guest User 1").ShouldBeSuccessful();
        
        AddItemToCart(cart, DefaultHostUserId, 20.00m);
        AddItemToCart(cart, DefaultGuestUserId1, 15.50m);
        
        return cart;
    }

    private static void AddItemToCart(TeamCart teamCart, UserId userId, decimal price)
    {
        var menuItemId = MenuItemId.CreateUnique();
        var menuCategoryId = MenuCategoryId.CreateUnique();
        var itemName = $"Test Item {price}";
        var basePrice = new Money(price, Currencies.Default);

        teamCart.AddItem(userId, menuItemId, menuCategoryId, itemName, basePrice, 1).ShouldBeSuccessful();
    }

    #endregion
}
