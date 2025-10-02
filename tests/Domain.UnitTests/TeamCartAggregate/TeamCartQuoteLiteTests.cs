using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UnitTests.TeamCartAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate;

[TestFixture]
public class TeamCartQuoteLiteTests
{
    [Test]
    public void ComputeQuoteLite_SplitsEvenly_AndPreservesSum()
    {
        var cart = TeamCartTestHelpers.CreateTeamCartWithGuest();
        var host = TeamCartTestHelpers.DefaultHostUserId;
        var guest = cart.Members.First(m => m.UserId != host).UserId;

        // Add items: host $12.30, guest $7.70
        var menuCategoryId = YummyZoom.Domain.MenuEntity.ValueObjects.MenuCategoryId.CreateUnique();
        var menuItemId = YummyZoom.Domain.MenuItemAggregate.ValueObjects.MenuItemId.CreateUnique();
        cart.AddItem(host, menuItemId, menuCategoryId, "Host Item", new Money(12.30m, Currencies.Default), 1);
        cart.AddItem(guest, menuItemId, menuCategoryId, "Guest Item", new Money(7.70m, Currencies.Default), 1);

        cart.LockForPayment(host).IsSuccess.Should().BeTrue();

        var currency = Currencies.Default;
        var memberSubtotals = new Dictionary<UserId, Money>
        {
            [host] = new Money(12.30m, currency),
            [guest] = new Money(7.70m, currency)
        };

        var fees = new Money(3.99m, currency);
        var tip = new Money(2.00m, currency);
        var tax = new Money(2.08m, currency);
        var discount = new Money(0m, currency);

        cart.ComputeQuoteLite(memberSubtotals, fees, tip, tax, discount).IsSuccess.Should().BeTrue();

        cart.QuoteVersion.Should().Be(1);
        cart.Status.Should().Be(TeamCartStatus.Locked);
        cart.MemberTotals.Should().ContainKey(host);
        cart.MemberTotals.Should().ContainKey(guest);

        var grand = 12.30m + 7.70m + 3.99m + 2.00m + 2.08m; // 28.07
        Math.Round(cart.GrandTotal.Amount, 2).Should().Be(Math.Round(grand, 2));

        var sumMembers = Math.Round(cart.MemberTotals[host].Amount + cart.MemberTotals[guest].Amount, 2);
        sumMembers.Should().Be(Math.Round(cart.GrandTotal.Amount, 2));
    }

    [Test]
    public void ComputeQuoteLite_ExcludesZeroItemMembers()
    {
        var cart = TeamCartTestHelpers.CreateTeamCartWithGuest();
        var host = TeamCartTestHelpers.DefaultHostUserId;
        var guest = cart.Members.First(m => m.UserId != host).UserId;

        var menuCategoryId = YummyZoom.Domain.MenuEntity.ValueObjects.MenuCategoryId.CreateUnique();
        var menuItemId = YummyZoom.Domain.MenuItemAggregate.ValueObjects.MenuItemId.CreateUnique();
        cart.AddItem(host, menuItemId, menuCategoryId, "Host Item", new Money(10.00m, Currencies.Default), 1);

        cart.LockForPayment(host).IsSuccess.Should().BeTrue();

        var currency = Currencies.Default;
        var memberSubtotals = new Dictionary<UserId, Money>
        {
            [host] = new Money(10.00m, currency),
            [guest] = new Money(0.00m, currency)
        };

        cart.ComputeQuoteLite(memberSubtotals, new Money(2.00m, currency), new Money(0m, currency), new Money(0m, currency), new Money(0m, currency))
            .IsSuccess.Should().BeTrue();

        cart.MemberTotals.Should().ContainKey(host);
        cart.MemberTotals.ContainsKey(guest).Should().BeFalse();
    }

    [Test]
    public void ComputeQuoteLite_DiscountCapped_AndGrandTotalNonNegative()
    {
        var cart = TeamCartTestHelpers.CreateTeamCartWithGuest();
        var host = TeamCartTestHelpers.DefaultHostUserId;
        var guest = cart.Members.First(m => m.UserId != host).UserId;

        var menuCategoryId = YummyZoom.Domain.MenuEntity.ValueObjects.MenuCategoryId.CreateUnique();
        var menuItemId = YummyZoom.Domain.MenuItemAggregate.ValueObjects.MenuItemId.CreateUnique();
        cart.AddItem(host, menuItemId, menuCategoryId, "Host Item", new Money(5.00m, Currencies.Default), 1);
        cart.AddItem(guest, menuItemId, menuCategoryId, "Guest Item", new Money(5.00m, Currencies.Default), 1);

        cart.LockForPayment(host).IsSuccess.Should().BeTrue();

        var currency = Currencies.Default;
        var memberSubtotals = new Dictionary<UserId, Money>
        {
            [host] = new Money(5.00m, currency),
            [guest] = new Money(5.00m, currency)
        };

        // Discount exceeds base (10.00)
        var discount = new Money(15.00m, currency);
        cart.ComputeQuoteLite(memberSubtotals, new Money(0m, currency), new Money(0m, currency), new Money(0m, currency), discount)
            .IsSuccess.Should().BeTrue();

        cart.GrandTotal.Amount.Should().BeGreaterOrEqualTo(0m);
        Math.Round(cart.MemberTotals.Values.Sum(m => m.Amount), 2).Should().Be(Math.Round(cart.GrandTotal.Amount, 2));
    }

    [Test]
    public void GetMemberQuote_BeforeAndAfterQuote()
    {
        var cart = TeamCartTestHelpers.CreateTeamCartWithGuest();
        var host = TeamCartTestHelpers.DefaultHostUserId;
        var guest = cart.Members.First(m => m.UserId != host).UserId;

        // Before quote
        cart.GetMemberQuote(host).IsFailure.Should().BeTrue();

        // Add items then lock
        var menuCategoryId = YummyZoom.Domain.MenuEntity.ValueObjects.MenuCategoryId.CreateUnique();
        var menuItemId = YummyZoom.Domain.MenuItemAggregate.ValueObjects.MenuItemId.CreateUnique();
        cart.AddItem(host, menuItemId, menuCategoryId, "Host Item", new Money(10.00m, Currencies.Default), 1);
        cart.AddItem(guest, menuItemId, menuCategoryId, "Guest Item", new Money(5.00m, Currencies.Default), 1);
        cart.LockForPayment(host).IsSuccess.Should().BeTrue();
        var currency = Currencies.Default;
        var subs = new Dictionary<UserId, Money>
        {
            [host] = new Money(10m, currency),
            [guest] = new Money(5m, currency)
        };
        cart.ComputeQuoteLite(subs, new Money(3m, currency), new Money(0m, currency), new Money(0m, currency), new Money(0m, currency))
            .IsSuccess.Should().BeTrue();

        var quoted = cart.GetMemberQuote(host);
        quoted.IsSuccess.Should().BeTrue();
        quoted.Value.Amount.Should().BeGreaterThan(0m);
    }
}
