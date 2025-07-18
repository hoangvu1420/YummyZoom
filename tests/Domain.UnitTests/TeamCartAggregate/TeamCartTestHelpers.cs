using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Entities;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate;

public abstract class TeamCartTestHelpers
{
    protected static readonly UserId DefaultHostUserId = UserId.CreateUnique();
    protected static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    protected const string DefaultHostName = "Host User";
    protected const string DefaultGuestName = "Guest User";
    protected static readonly DateTime DefaultDeadline = DateTime.UtcNow.AddHours(2);
    
    protected static TeamCart CreateValidTeamCart()
    {
        return TeamCart.Create(
            DefaultHostUserId,
            DefaultRestaurantId,
            DefaultHostName,
            DefaultDeadline).Value;
    }
    
    protected static TeamCart CreateTeamCartWithGuest()
    {
        var teamCart = CreateValidTeamCart();
        var guestUserId = UserId.CreateUnique();
        var result = teamCart.AddMember(guestUserId, DefaultGuestName);
        result.ShouldBeSuccessful(); // Ensure the addition was successful
        return teamCart;
    }
    
    protected static TeamCart CreateExpiredTeamCart()
    {
        // Create a valid team cart first (with a future deadline)
        var teamCart = CreateValidTeamCart();
        
        // Use reflection to set the ExpiresAt and Deadline to a past date
        // This is necessary because we can't directly set these properties
        var pastDate = DateTime.UtcNow.AddHours(-1);
        typeof(TeamCart).GetProperty("ExpiresAt")?.SetValue(teamCart, pastDate);
        typeof(TeamCart).GetProperty("Deadline")?.SetValue(teamCart, pastDate);
        
        // Explicitly mark as expired
        var result = teamCart.MarkAsExpired();
        result.ShouldBeSuccessful(); // Ensure the transition was successful
        return teamCart;
    }
    
    #region Phase 4 Helper Methods

    /// <summary>
    /// Creates a team cart with all payment information collected - ready for conversion
    /// </summary>
    protected static TeamCart CreateTeamCartReadyForConversion()
    {
        var teamCart = CreateTeamCartWithGuest();
        
        // Add some items first
        var menuItemId = MenuItemId.CreateUnique();
        var menuCategoryId = MenuCategoryId.CreateUnique();
        
        teamCart.AddItem(DefaultHostUserId, menuItemId, menuCategoryId, "Host Item", 
            new Money(25.00m, Currencies.Default), 1);
        teamCart.AddItem(teamCart.Members.First(m => m.UserId != DefaultHostUserId).UserId, 
            menuItemId, menuCategoryId, "Guest Item", new Money(30.00m, Currencies.Default), 1);
        
        // Initiate checkout
        var checkoutResult = teamCart.InitiateCheckout(DefaultHostUserId);
        checkoutResult.ShouldBeSuccessful();
        
        // Add payment information using the new payment methods
        var hostResult = teamCart.RecordSuccessfulOnlinePayment(DefaultHostUserId, new Money(25.00m, Currencies.Default), "txn_host_123");
        hostResult.ShouldBeSuccessful();
        
        var guestUserId = teamCart.Members.First(m => m.UserId != DefaultHostUserId).UserId;
        var guestResult = teamCart.RecordSuccessfulOnlinePayment(guestUserId, new Money(30.00m, Currencies.Default), "txn_guest_456");
        guestResult.ShouldBeSuccessful();
        
        return teamCart;
    }

    /// <summary>
    /// Creates a team cart with partial payment information - not ready for conversion
    /// </summary>
    protected static TeamCart CreateTeamCartWithPartialPayment()
    {
        var teamCart = CreateTeamCartWithGuest();
        
        // Add some items first
        var menuItemId = MenuItemId.CreateUnique();
        var menuCategoryId = MenuCategoryId.CreateUnique();
        
        teamCart.AddItem(DefaultHostUserId, menuItemId, menuCategoryId, "Host Item", 
            new Money(25.00m, Currencies.Default), 1);
        teamCart.AddItem(teamCart.Members.First(m => m.UserId != DefaultHostUserId).UserId, 
            menuItemId, menuCategoryId, "Guest Item", new Money(30.00m, Currencies.Default), 1);
        
        // Initiate checkout
        var checkoutResult = teamCart.InitiateCheckout(DefaultHostUserId);
        checkoutResult.ShouldBeSuccessful();
        
        // Add payment information to only the host member
        var hostResult = teamCart.RecordSuccessfulOnlinePayment(DefaultHostUserId, new Money(25.00m, Currencies.Default), "txn_host_123");
        hostResult.ShouldBeSuccessful();
        
        return teamCart;
    }

    /// <summary>
    /// Creates a team cart with Cash on Delivery payment method
    /// </summary>
    protected static TeamCart CreateTeamCartWithCODPayment()
    {
        var teamCart = CreateTeamCartWithGuest();
        
        // Add some items first
        var menuItemId = MenuItemId.CreateUnique();
        var menuCategoryId = MenuCategoryId.CreateUnique();
        
        teamCart.AddItem(DefaultHostUserId, menuItemId, menuCategoryId, "Host Item", 
            new Money(25.00m, Currencies.Default), 1);
        teamCart.AddItem(teamCart.Members.First(m => m.UserId != DefaultHostUserId).UserId, 
            menuItemId, menuCategoryId, "Guest Item", new Money(30.00m, Currencies.Default), 1);
        
        // Initiate checkout
        var checkoutResult = teamCart.InitiateCheckout(DefaultHostUserId);
        checkoutResult.ShouldBeSuccessful();
        
        // Add COD payment (both members)
        var hostResult = teamCart.CommitToCashOnDelivery(DefaultHostUserId, new Money(25.00m, Currencies.Default));
        hostResult.ShouldBeSuccessful();
        
        var guestUserId = teamCart.Members.First(m => m.UserId != DefaultHostUserId).UserId;
        var guestResult = teamCart.CommitToCashOnDelivery(guestUserId, new Money(30.00m, Currencies.Default));
        guestResult.ShouldBeSuccessful();
        
        return teamCart;
    }

    /// <summary>
    /// Creates a team cart that has already been converted to order
    /// </summary>
    protected static TeamCart CreateConvertedTeamCart()
    {
        var teamCart = CreateTeamCartReadyForConversion();
        
        // Mark as converted
        var result = teamCart.MarkAsConverted();
        result.ShouldBeSuccessful();
        
        return teamCart;
    }

    /// <summary>
    /// Creates sample payment transactions for testing
    /// </summary>
    protected static List<PaymentTransaction> CreateSamplePaymentTransactions()
    {
        var transactions = new List<PaymentTransaction>();
        
        // Host payment
        var hostPayment = PaymentTransaction.Create(
            PaymentMethodType.CreditCard,
            PaymentTransactionType.Payment,
            new Money(25.00m, Currencies.Default),
            DateTime.UtcNow,
            "Visa ending in 1234",
            "stripe_pi_host_123",
            DefaultHostUserId);
        transactions.Add(hostPayment.Value);
        
        // Guest payment
        var guestUserId = UserId.CreateUnique();
        var guestPayment = PaymentTransaction.Create(
            PaymentMethodType.CreditCard,
            PaymentTransactionType.Payment,
            new Money(30.00m, Currencies.Default),
            DateTime.UtcNow,
            "Mastercard ending in 5678",
            "stripe_pi_guest_456",
            guestUserId);
        transactions.Add(guestPayment.Value);
        
        return transactions;
    }

    /// <summary>
    /// Creates payment transactions that match the team cart's payment structure
    /// </summary>
    protected static List<PaymentTransaction> CreatePaymentTransactionsForTeamCart(TeamCart teamCart)
    {
        var transactions = new List<PaymentTransaction>();
        
        foreach (var memberPayment in teamCart.MemberPayments)
        {
            var transaction = PaymentTransaction.Create(
                PaymentMethodType.CreditCard,
                PaymentTransactionType.Payment,
                memberPayment.Amount,
                DateTime.UtcNow,
                "Online Payment",
                $"stripe_pi_{memberPayment.UserId.Value}",
                memberPayment.UserId);
            transactions.Add(transaction.Value);
        }
        
        return transactions;
    }

    /// <summary>
    /// Creates COD payment transactions (host as guarantor)
    /// </summary>
    protected static List<PaymentTransaction> CreateCODPaymentTransactions(TeamCart teamCart)
    {
        var transactions = new List<PaymentTransaction>();
        
        var totalAmount = teamCart.MemberPayments.Sum(mp => mp.Amount.Amount);
        
        var codTransaction = PaymentTransaction.Create(
            PaymentMethodType.CashOnDelivery,
            PaymentTransactionType.Payment,
            new Money(totalAmount, Currencies.Default),
            DateTime.UtcNow,
            "Cash on Delivery",
            null,
            teamCart.HostUserId);
        transactions.Add(codTransaction.Value);
        
        return transactions;
    }

    #endregion
}
