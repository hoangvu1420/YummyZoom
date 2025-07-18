using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.Events;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate;

[TestFixture]
public class TeamCartPaymentTests
{
    private TeamCart _teamCart = null!;
    private UserId _hostUserId = null!;
    private UserId _guestUserId1 = null!;
    private UserId _guestUserId2 = null!;
    private RestaurantId _restaurantId = null!;
    private MenuItemId _menuItemId1 = null!;
    private MenuItemId _menuItemId2 = null!;
    private MenuCategoryId _menuCategoryId = null!;
    private Money _hostItemAmount = null!;
    private Money _guest1ItemAmount = null!;
    private Money _guest2ItemAmount = null!;

    [SetUp]
    public void SetUp()
    {
        _hostUserId = UserId.CreateUnique();
        _guestUserId1 = UserId.CreateUnique();
        _guestUserId2 = UserId.CreateUnique();
        _restaurantId = RestaurantId.CreateUnique();
        _menuItemId1 = MenuItemId.CreateUnique();
        _menuItemId2 = MenuItemId.CreateUnique();
        _menuCategoryId = MenuCategoryId.CreateUnique();
        _hostItemAmount = new Money(10.00m, Currencies.Default);
        _guest1ItemAmount = new Money(15.00m, Currencies.Default);
        _guest2ItemAmount = new Money(20.00m, Currencies.Default);

        // Create a team cart and add members
        _teamCart = TeamCart.Create(_hostUserId, _restaurantId, "Host User").Value;
        _teamCart.AddMember(_guestUserId1, "Guest User 1");
        _teamCart.AddMember(_guestUserId2, "Guest User 2");

        // Add items to the cart
        _teamCart.AddItem(
            _hostUserId,
            _menuItemId1,
            _menuCategoryId,
            "Host Item",
            _hostItemAmount,
            1);

        _teamCart.AddItem(
            _guestUserId1,
            _menuItemId2,
            _menuCategoryId,
            "Guest 1 Item",
            _guest1ItemAmount,
            1);
            
        _teamCart.AddItem(
            _guestUserId2,
            _menuItemId1,
            _menuCategoryId,
            "Guest 2 Item",
            _guest2ItemAmount,
            1);

        // Initiate checkout to move to AwaitingPayments status
        _teamCart.InitiateCheckout(_hostUserId);
    }

    #region Checkout Tests

    [Test]
    public void InitiateCheckout_ByHost_ShouldSucceed()
    {
        // Arrange - Create a new cart to test checkout
        var newCart = CreateCartWithItems();

        // Act
        var result = newCart.InitiateCheckout(_hostUserId);

        // Assert
        result.ShouldBeSuccessful();
        newCart.Status.Should().Be(TeamCartStatus.AwaitingPayments);
    }

    [Test]
    public void InitiateCheckout_ByGuest_ShouldFail()
    {
        // Arrange - Create a new cart to test checkout
        var newCart = CreateCartWithItems();

        // Act
        var result = newCart.InitiateCheckout(_guestUserId1);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.OnlyHostCanInitiateCheckout);
        newCart.Status.Should().Be(TeamCartStatus.Open);
    }

    [Test]
    public void InitiateCheckout_WithoutItems_ShouldFail()
    {
        // Arrange - Create cart without items
        var emptyCartResult = TeamCart.Create(_hostUserId, _restaurantId, "Host User");
        var emptyCart = emptyCartResult.Value;
        emptyCart.AddMember(_guestUserId1, "Guest User");

        // Act
        var result = emptyCart.InitiateCheckout(_hostUserId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CannotInitiateCheckoutWithoutItems);
    }

    [Test]
    public void InitiateCheckout_WithOnlyHost_ShouldFail()
    {
        // Arrange - Create cart with only host
        var hostOnlyCartResult = TeamCart.Create(_hostUserId, _restaurantId, "Host User");
        var hostOnlyCart = hostOnlyCartResult.Value;
        AddItemToCart(hostOnlyCart, _hostUserId, 20.00m);

        // Act
        var result = hostOnlyCart.InitiateCheckout(_hostUserId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CannotInitiateCheckoutWithoutMembers);
    }

    #endregion

    #region CommitToCashOnDelivery Tests

    [Test]
    public void CommitToCashOnDelivery_WithValidData_ShouldSucceed()
    {
        // Act
        var result = _teamCart.CommitToCashOnDelivery(_hostUserId, _hostItemAmount);

        // Assert
        result.ShouldBeSuccessful();
        var payment = _teamCart.MemberPayments.FirstOrDefault(p => p.UserId == _hostUserId);
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
        var result = _teamCart.CommitToCashOnDelivery(_hostUserId, invalidAmount);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidPaymentAmount);
        _teamCart.MemberPayments.Should().NotContain(p => p.UserId == _hostUserId);
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
    public void CommitToCashOnDelivery_WhenCartNotInAwaitingPaymentsStatus_ShouldFail()
    {
        // Arrange
        var newCart = TeamCart.Create(_hostUserId, _restaurantId, "Host User").Value;

        // Act
        var result = newCart.CommitToCashOnDelivery(_hostUserId, _hostItemAmount);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CannotCommitPaymentInCurrentStatus);
        newCart.MemberPayments.Should().BeEmpty();
    }

    [Test]
    public void CommitToCashOnDelivery_ShouldReplaceExistingPayment()
    {
        // Arrange - First record a successful online payment
        _teamCart.RecordSuccessfulOnlinePayment(_hostUserId, _hostItemAmount, "txn_123");
        var initialPaymentCount = _teamCart.MemberPayments.Count;

        // Act - Then switch to COD
        var result = _teamCart.CommitToCashOnDelivery(_hostUserId, _hostItemAmount);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.MemberPayments.Count.Should().Be(initialPaymentCount); // Count should remain the same
        var payment = _teamCart.MemberPayments.FirstOrDefault(p => p.UserId == _hostUserId);
        payment.Should().NotBeNull();
        payment!.Method.Should().Be(PaymentMethod.CashOnDelivery); // Method should be updated
    }

    [Test]
    public void CommitToCashOnDelivery_AllMembersCommitted_ShouldTransitionToReadyToConfirm()
    {
        // Arrange - Other members commit to payment
        _teamCart.RecordSuccessfulOnlinePayment(_guestUserId1, _guest1ItemAmount, "txn_123");
        _teamCart.CommitToCashOnDelivery(_guestUserId2, _guest2ItemAmount);

        // Act - Last member commits
        var result = _teamCart.CommitToCashOnDelivery(_hostUserId, _hostItemAmount);

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.Status.Should().Be(TeamCartStatus.ReadyToConfirm);
    }

    [Test]
    public void CommitToCashOnDelivery_RaisesCorrectDomainEvent()
    {
        // Act
        _teamCart.CommitToCashOnDelivery(_hostUserId, _hostItemAmount);

        // Assert
        var paymentEvent = _teamCart.DomainEvents
            .OfType<MemberCommittedToPayment>()
            .FirstOrDefault();
        paymentEvent.Should().NotBeNull();
        paymentEvent!.UserId.Should().Be(_hostUserId);
        paymentEvent.Method.Should().Be(PaymentMethod.CashOnDelivery);
        paymentEvent.Amount.Should().Be(_hostItemAmount);
    }

    #endregion

    #region RecordSuccessfulOnlinePayment Tests

    [Test]
    public void RecordSuccessfulOnlinePayment_WithValidData_ShouldSucceed()
    {
        // Act
        var result = _teamCart.RecordSuccessfulOnlinePayment(_hostUserId, _hostItemAmount, "txn_123");

        // Assert
        result.ShouldBeSuccessful();
        var payment = _teamCart.MemberPayments.FirstOrDefault(p => p.UserId == _hostUserId);
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
        var result = _teamCart.RecordSuccessfulOnlinePayment(_hostUserId, invalidAmount, "txn_123");

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidPaymentAmount);
        _teamCart.MemberPayments.Should().NotContain(p => p.UserId == _hostUserId);
    }

    [Test]
    public void RecordSuccessfulOnlinePayment_WithEmptyTransactionId_ShouldFail()
    {
        // Act
        var result = _teamCart.RecordSuccessfulOnlinePayment(_hostUserId, _hostItemAmount, "");

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.InvalidTransactionId);
        _teamCart.MemberPayments.Should().NotContain(p => p.UserId == _hostUserId);
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
    public void RecordSuccessfulOnlinePayment_WhenCartNotInAwaitingPaymentsStatus_ShouldFail()
    {
        // Arrange
        var newCart = TeamCart.Create(_hostUserId, _restaurantId, "Host User").Value;

        // Act
        var result = newCart.RecordSuccessfulOnlinePayment(_hostUserId, _hostItemAmount, "txn_123");

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(TeamCartErrors.CannotCommitPaymentInCurrentStatus);
        newCart.MemberPayments.Should().BeEmpty();
    }

    [Test]
    public void RecordSuccessfulOnlinePayment_ShouldReplaceExistingCODPayment()
    {
        // Arrange - First commit to COD
        _teamCart.CommitToCashOnDelivery(_hostUserId, _hostItemAmount);
        var initialPaymentCount = _teamCart.MemberPayments.Count;

        // Act - Then switch to online payment
        var result = _teamCart.RecordSuccessfulOnlinePayment(_hostUserId, _hostItemAmount, "txn_123");

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.MemberPayments.Count.Should().Be(initialPaymentCount); // Count should remain the same
        var payment = _teamCart.MemberPayments.FirstOrDefault(p => p.UserId == _hostUserId);
        payment.Should().NotBeNull();
        payment!.Method.Should().Be(PaymentMethod.Online); // Method should be updated
        payment.Status.Should().Be(PaymentStatus.PaidOnline); // Status should be updated
    }

    [Test]
    public void RecordSuccessfulOnlinePayment_AllMembersCommitted_ShouldTransitionToReadyToConfirm()
    {
        // Arrange - Other members commit to payment
        _teamCart.CommitToCashOnDelivery(_guestUserId1, _guest1ItemAmount);
        _teamCart.CommitToCashOnDelivery(_guestUserId2, _guest2ItemAmount);

        // Act - Last member commits with online payment
        var result = _teamCart.RecordSuccessfulOnlinePayment(_hostUserId, _hostItemAmount, "txn_123");

        // Assert
        result.ShouldBeSuccessful();
        _teamCart.Status.Should().Be(TeamCartStatus.ReadyToConfirm);
    }

    [Test]
    public void RecordSuccessfulOnlinePayment_RaisesCorrectDomainEvent()
    {
        // Act
        _teamCart.RecordSuccessfulOnlinePayment(_hostUserId, _hostItemAmount, "txn_123");

        // Assert
        var paymentEvent = _teamCart.DomainEvents
            .OfType<OnlinePaymentSucceeded>()
            .FirstOrDefault();
        paymentEvent.Should().NotBeNull();
        paymentEvent!.UserId.Should().Be(_hostUserId);
        paymentEvent.TransactionId.Should().Be("txn_123");
        paymentEvent.Amount.Should().Be(_hostItemAmount);
    }

    #endregion

    #region Payment Workflow Tests

    [Test]
    public void PaymentWorkflow_AllOnlinePayments_ShouldTransitionToReadyToConfirm()
    {
        // Act - Complete all online payments
        _teamCart.RecordSuccessfulOnlinePayment(_hostUserId, _hostItemAmount, "txn_host");
        _teamCart.RecordSuccessfulOnlinePayment(_guestUserId1, _guest1ItemAmount, "txn_guest1");
        _teamCart.RecordSuccessfulOnlinePayment(_guestUserId2, _guest2ItemAmount, "txn_guest2");

        // Assert
        _teamCart.Status.Should().Be(TeamCartStatus.ReadyToConfirm);

        // Check domain event
        var domainEvent = _teamCart.DomainEvents
            .OfType<TeamCartReadyForConfirmation>()
            .FirstOrDefault();
        domainEvent.Should().NotBeNull();
        domainEvent!.TotalAmount.Amount.Should().Be(45.00m); // Sum of all payments
        domainEvent.CashAmount.Amount.Should().Be(0m); // No cash payments
    }

    [Test]
    public void PaymentWorkflow_AllCODPayments_ShouldTransitionToReadyToConfirm()
    {
        // Act - Commit all to COD
        _teamCart.CommitToCashOnDelivery(_hostUserId, _hostItemAmount);
        _teamCart.CommitToCashOnDelivery(_guestUserId1, _guest1ItemAmount);
        _teamCart.CommitToCashOnDelivery(_guestUserId2, _guest2ItemAmount);

        // Assert
        _teamCart.Status.Should().Be(TeamCartStatus.ReadyToConfirm);

        // Check domain event
        var domainEvent = _teamCart.DomainEvents
            .OfType<TeamCartReadyForConfirmation>()
            .FirstOrDefault();
        domainEvent.Should().NotBeNull();
        domainEvent!.TotalAmount.Amount.Should().Be(45.00m); // Sum of all payments
        domainEvent.CashAmount.Amount.Should().Be(45.00m); // All cash payments
    }

    [Test]
    public void PaymentWorkflow_MixedPayments_ShouldTransitionToReadyToConfirm()
    {
        // Act - One online, two COD
        _teamCart.RecordSuccessfulOnlinePayment(_hostUserId, _hostItemAmount, "txn_host");
        _teamCart.CommitToCashOnDelivery(_guestUserId1, _guest1ItemAmount);
        _teamCart.CommitToCashOnDelivery(_guestUserId2, _guest2ItemAmount);

        // Assert
        _teamCart.Status.Should().Be(TeamCartStatus.ReadyToConfirm);

        // Check domain event
        var domainEvent = _teamCart.DomainEvents
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
        var cart = TeamCart.Create(_hostUserId, _restaurantId, "Host User").Value;
        cart.AddMember(_guestUserId1, "Guest User 1");
        
        AddItemToCart(cart, _hostUserId, 20.00m);
        AddItemToCart(cart, _guestUserId1, 15.50m);
        
        return cart;
    }

    private static void AddItemToCart(TeamCart teamCart, UserId userId, decimal price)
    {
        var menuItemId = MenuItemId.CreateUnique();
        var menuCategoryId = MenuCategoryId.CreateUnique();
        var itemName = $"Test Item {price}";
        var basePrice = new Money(price, Currencies.Default);

        teamCart.AddItem(userId, menuItemId, menuCategoryId, itemName, basePrice, 1);
    }

    #endregion
}