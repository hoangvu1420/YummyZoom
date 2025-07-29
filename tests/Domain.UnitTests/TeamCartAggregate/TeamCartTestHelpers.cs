using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Entities;
using System.Reflection;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate;

public static class TeamCartTestHelpers
{
    public static readonly UserId DefaultHostUserId = UserId.CreateUnique();
    public static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    public const string DefaultHostName = "Host User";
    public const string DefaultGuestName = "Guest User";
    public static readonly UserId DefaultGuestUserId1 = UserId.CreateUnique();
    public static readonly UserId DefaultGuestUserId2 = UserId.CreateUnique();
    public static readonly DateTime DefaultDeadline = DateTime.UtcNow.AddHours(2);
    
    public static TeamCart CreateValidTeamCart()
    {
        return TeamCart.Create(
            DefaultHostUserId,
            DefaultRestaurantId,
            DefaultHostName,
            DefaultDeadline).Value;
    }
    
    public static TeamCart CreateTeamCartWithGuest()
    {
        var teamCart = CreateValidTeamCart();
        var guestUserId = UserId.CreateUnique();
        var result = teamCart.AddMember(guestUserId, DefaultGuestName);
        result.IsSuccess.Should().BeTrue(); // Ensure the addition was successful
        return teamCart;
    }
    
    public static TeamCart CreateExpiredTeamCart()
    {
        var teamCart = CreateValidTeamCart();
        var pastDate = DateTime.UtcNow.AddHours(-1);
        typeof(TeamCart).GetProperty(nameof(TeamCart.ExpiresAt))!.SetValue(teamCart, pastDate);
        typeof(TeamCart).GetProperty(nameof(TeamCart.Deadline))!.SetValue(teamCart, pastDate);
        var result = teamCart.MarkAsExpired();
        result.IsSuccess.Should().BeTrue();
        return teamCart;
    }

    public static TeamCart CreateTeamCartReadyForConversion()
    {
        var teamCart = CreateTeamCartWithGuest();
        
        var menuItemId = MenuItemId.CreateUnique();
        var menuCategoryId = MenuCategoryId.CreateUnique();
        
        teamCart.AddItem(DefaultHostUserId, menuItemId, menuCategoryId, "Host Item", 
            new Money(25.00m, Currencies.Default), 1);
        teamCart.AddItem(teamCart.Members.First(m => m.UserId != DefaultHostUserId).UserId, 
            menuItemId, menuCategoryId, "Guest Item", new Money(30.00m, Currencies.Default), 1);
        
        var lockResult = teamCart.LockForPayment(DefaultHostUserId);
        lockResult.IsSuccess.Should().BeTrue();
        
        var hostResult = teamCart.RecordSuccessfulOnlinePayment(DefaultHostUserId, new Money(25.00m, Currencies.Default), "txn_host_123");
        hostResult.IsSuccess.Should().BeTrue();
        
        var guestUserId = teamCart.Members.First(m => m.UserId != DefaultHostUserId).UserId;
        var guestResult = teamCart.RecordSuccessfulOnlinePayment(guestUserId, new Money(30.00m, Currencies.Default), "txn_guest_456");
        guestResult.IsSuccess.Should().BeTrue();
        
        return teamCart;
    }

    public static TeamCart CreateTeamCartWithPartialPayment()
    {
        var teamCart = CreateTeamCartWithGuest();
        
        var menuItemId = MenuItemId.CreateUnique();
        var menuCategoryId = MenuCategoryId.CreateUnique();
        
        teamCart.AddItem(DefaultHostUserId, menuItemId, menuCategoryId, "Host Item", 
            new Money(25.00m, Currencies.Default), 1);
        teamCart.AddItem(teamCart.Members.First(m => m.UserId != DefaultHostUserId).UserId, 
            menuItemId, menuCategoryId, "Guest Item", new Money(30.00m, Currencies.Default), 1);
        
        var lockResult = teamCart.LockForPayment(DefaultHostUserId);
        lockResult.IsSuccess.Should().BeTrue();
        
        var hostResult = teamCart.RecordSuccessfulOnlinePayment(DefaultHostUserId, new Money(25.00m, Currencies.Default), "txn_host_123");
        hostResult.IsSuccess.Should().BeTrue();
        
        return teamCart;
    }

    public static TeamCart CreateTeamCartWithCODPayment()
    {
        var teamCart = CreateTeamCartWithGuest();
        
        var menuItemId = MenuItemId.CreateUnique();
        var menuCategoryId = MenuCategoryId.CreateUnique();
        
        teamCart.AddItem(DefaultHostUserId, menuItemId, menuCategoryId, "Host Item", 
            new Money(25.00m, Currencies.Default), 1);
        teamCart.AddItem(teamCart.Members.First(m => m.UserId != DefaultHostUserId).UserId, 
            menuItemId, menuCategoryId, "Guest Item", new Money(30.00m, Currencies.Default), 1);
        
        var lockResult = teamCart.LockForPayment(DefaultHostUserId);
        lockResult.IsSuccess.Should().BeTrue();
        
        var hostResult = teamCart.CommitToCashOnDelivery(DefaultHostUserId, new Money(25.00m, Currencies.Default));
        hostResult.IsSuccess.Should().BeTrue();
        
        var guestUserId = teamCart.Members.First(m => m.UserId != DefaultHostUserId).UserId;
        var guestResult = teamCart.CommitToCashOnDelivery(guestUserId, new Money(30.00m, Currencies.Default));
        guestResult.IsSuccess.Should().BeTrue();
        
        return teamCart;
    }

    public static TeamCart CreateConvertedTeamCart()
    {
        var teamCart = CreateTeamCartReadyForConversion();
        var result = teamCart.MarkAsConverted();
        result.IsSuccess.Should().BeTrue();
        return teamCart;
    }
    
    public static List<PaymentTransaction> CreatePaymentTransactionsForTeamCart(TeamCart teamCart)
    {
        var transactions = new List<PaymentTransaction>();
        
        foreach (var memberPayment in teamCart.MemberPayments)
        {
            var transaction = PaymentTransaction.Create(
                PaymentMethodType.CreditCard,
                PaymentTransactionType.Payment,
                memberPayment.Amount,
                DateTime.UtcNow,
                paidByUserId: memberPayment.UserId).Value;
            transactions.Add(transaction);
        }
        
        return transactions;
    }

    public static TeamCart CreateTeamCartReadyForConversionWithCODPayment()
    {
        var teamCart = CreateTeamCartWithCODPayment();
        teamCart.Status.Should().Be(TeamCartStatus.ReadyToConfirm);
        return teamCart;
    }
    
    public static TeamCart CreateTeamCartReadyForConversionWithPartialPayment()
    {
        var teamCart = CreateTeamCartWithPartialPayment();
        var guestUserId = teamCart.Members.First(m => m.UserId != DefaultHostUserId).UserId;
        var guestResult = teamCart.RecordSuccessfulOnlinePayment(guestUserId, new Money(30.00m, Currencies.Default), "txn_guest_456");
        guestResult.IsSuccess.Should().BeTrue();
        teamCart.Status.Should().Be(TeamCartStatus.ReadyToConfirm);
        return teamCart;
    }
    
    public static TeamCart CreateTeamCartWithMultipleItems()
    {
        var teamCart = CreateTeamCartWithGuest();
        
        var menuItemId1 = MenuItemId.CreateUnique();
        var menuItemId2 = MenuItemId.CreateUnique();
        var menuItemId3 = MenuItemId.CreateUnique();
        var menuCategoryId = MenuCategoryId.CreateUnique();
        
        teamCart.AddItem(DefaultHostUserId, menuItemId1, menuCategoryId, "Pizza Margherita", new Money(15.00m, Currencies.Default), 2);
        teamCart.AddItem(DefaultHostUserId, menuItemId2, menuCategoryId, "Caesar Salad", new Money(12.00m, Currencies.Default), 1);
        teamCart.AddItem(teamCart.Members.First(m => m.UserId != DefaultHostUserId).UserId, menuItemId3, menuCategoryId, "Pasta Carbonara", new Money(18.00m, Currencies.Default), 1);
        
        return teamCart;
    }
    
    public static TeamCart CreateTeamCartWithCustomizations()
    {
        var teamCart = CreateTeamCartWithGuest();
        
        var menuItemId = MenuItemId.CreateUnique();
        var menuCategoryId = MenuCategoryId.CreateUnique();
        
        var customizations = new List<TeamCartItemCustomization>
        {
            TeamCartItemCustomization.Create("Size", "Extra Cheese", new Money(2.50m, Currencies.Default)).Value,
            TeamCartItemCustomization.Create("Toppings", "Pepperoni", new Money(3.00m, Currencies.Default)).Value
        };
        
        var addResult = teamCart.AddItem(DefaultHostUserId, menuItemId, menuCategoryId, "Custom Pizza", new Money(20.00m, Currencies.Default), 1, customizations);
        addResult.IsSuccess.Should().BeTrue();
        
        return teamCart;
    }
    
    public static TeamCart CreateAlreadyConvertedTeamCart()
    {
        var teamCart = CreateTeamCartReadyForConversion();
        var result = teamCart.MarkAsConverted();
        result.IsSuccess.Should().BeTrue();
        return teamCart;
    }

    /// <summary>
    /// Creates a TeamCart in a ReadyToConfirm state but with no items.
    /// This is an invalid state used to test boundary conditions of the conversion service.
    /// We properly set up payments but bypass the items requirement to test the Order validation.
    /// </summary>
    public static TeamCart CreateReadyForConversionCartWithNoItems()
    {
        var teamCart = CreateTeamCartWithGuest();
        
        // Add items for both members temporarily to allow locking and payment setup
        var menuItemId = MenuItemId.CreateUnique();
        var menuCategoryId = MenuCategoryId.CreateUnique();
        teamCart.AddItem(DefaultHostUserId, menuItemId, menuCategoryId, "Host Temp Item", 
            new Money(25.00m, Currencies.Default), 1);
        
        var guestUserId = teamCart.Members.First(m => m.UserId != DefaultHostUserId).UserId;
        teamCart.AddItem(guestUserId, menuItemId, menuCategoryId, "Guest Temp Item", 
            new Money(30.00m, Currencies.Default), 1);
        
        // Lock the cart for payment
        var lockResult = teamCart.LockForPayment(DefaultHostUserId);
        lockResult.IsSuccess.Should().BeTrue();
        
        // Set up payments for both members (amounts must match their item totals)
        var hostResult = teamCart.RecordSuccessfulOnlinePayment(DefaultHostUserId, new Money(25.00m, Currencies.Default), "txn_host_123");
        hostResult.IsSuccess.Should().BeTrue();
        
        var guestResult = teamCart.RecordSuccessfulOnlinePayment(guestUserId, new Money(30.00m, Currencies.Default), "txn_guest_456");
        guestResult.IsSuccess.Should().BeTrue();
        
        // Now use reflection to remove all items while keeping the ReadyToConfirm status and payments
        var itemsField = typeof(TeamCart).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var itemsList = (List<TeamCartItem>)itemsField.GetValue(teamCart)!;
        itemsList.Clear();

        teamCart.Status.Should().Be(TeamCartStatus.ReadyToConfirm);
        teamCart.Items.Should().BeEmpty();
        teamCart.MemberPayments.Should().NotBeEmpty(); // Ensure payments are still there
    
        return teamCart;
    }
}
