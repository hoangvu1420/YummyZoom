using FluentAssertions;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.ApplyCouponToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.ApplyTipToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment;
using YummyZoom.Application.TeamCarts.Commands.ConvertTeamCartToOrder;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.FullFlow;

/// <summary>
/// Tests around locked-only financial adjustments (tip, coupon) and their effect on conversion totals.
/// </summary>
public class TeamCartCouponAndTipFlowTests : BaseTestFixture
{
    [Test]
    public async Task ApplyTipAndCoupon_AfterLock_AffectsTotals_AndConverts()
    {
        // Arrange: Single-member host cart
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();
        await DrainOutboxAsync();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);

        await scenario.ActAsHost();
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 2))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Lock then apply tip and coupon (locked-only)
        (await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        var tipAmount = 5.00m;
        (await SendAsync(new ApplyTipToTeamCartCommand(scenario.TeamCartId, tipAmount))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        var couponCode = Testing.TestData.DefaultCouponCode;
        (await SendAsync(new ApplyCouponToTeamCartCommand(scenario.TeamCartId, couponCode))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Commit COD for host to reach ReadyToConfirm
        (await SendAsync(new CommitToCodPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Assert cart state pre-conversion
        var cart = await Testing.FindTeamCartAsync(TeamCartId.Create(scenario.TeamCartId));
        cart.Should().NotBeNull();
        cart!.Status.Should().Be(TeamCartStatus.ReadyToConfirm);

        // Convert to order
        var convert = await SendAsync(new ConvertTeamCartToOrderCommand(
            scenario.TeamCartId,
            Street: "123 Main St",
            City: "City",
            State: "CA",
            ZipCode: "90210",
            Country: "US",
            SpecialInstructions: null));

        convert.IsSuccess.Should().BeTrue();
        var orderId = OrderId.Create(convert.Value.OrderId);

        var order = await FindOrderAsync(orderId);
        order.Should().NotBeNull();
        order!.AppliedCouponId.Should().NotBeNull();
        order.TipAmount.Amount.Should().Be(tipAmount);
        order.DiscountAmount.Amount.Should().BeGreaterThan(0);

        // Verify financial identity: total = subtotal - discount + delivery + tip + tax
        var recomputed = order.Subtotal.Amount
                        - order.DiscountAmount.Amount
                        + order.DeliveryFee.Amount
                        + order.TipAmount.Amount
                        + order.TaxAmount.Amount;
        order.TotalAmount.Amount.Should().BeApproximately(recomputed, 0.01m);

        // Cart converted
        cart = await Testing.FindTeamCartAsync(TeamCartId.Create(scenario.TeamCartId));
        cart!.Status.Should().Be(TeamCartStatus.Converted);
    }

    [Test]
    public async Task ApplyTipAndCoupon_BeforeLock_ShouldFail()
    {
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();
        await DrainOutboxAsync();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await scenario.ActAsHost();
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        var tip = await SendAsync(new ApplyTipToTeamCartCommand(scenario.TeamCartId, 3.00m));
        tip.IsFailure.Should().BeTrue();
        tip.Error.Code.Should().Be("TeamCart.CanOnlyApplyFinancialsToLockedCart");

        var coupon = await SendAsync(new ApplyCouponToTeamCartCommand(scenario.TeamCartId, Testing.TestData.DefaultCouponCode));
        coupon.IsFailure.Should().BeTrue();
        coupon.Error.Code.Should().Be("TeamCart.CanOnlyApplyFinancialsToLockedCart");

        var cart = await Testing.FindTeamCartAsync(TeamCartId.Create(scenario.TeamCartId));
        cart!.Status.Should().Be(TeamCartStatus.Open);
    }

    [Test]
    public async Task ApplyTipAndCoupon_AsGuest_ShouldFail()
    {
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .WithGuest("Guest A")
            .BuildAsync();
        await DrainOutboxAsync();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await scenario.ActAsHost();
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        await scenario.ActAsGuest("Guest A");
        var tip = await SendAsync(new ApplyTipToTeamCartCommand(scenario.TeamCartId, 2.50m));
        tip.IsFailure.Should().BeTrue();
        tip.Error.Code.Should().Be("TeamCart.OnlyHostCanModifyFinancials");

        await FluentActions.Invoking(() => SendAsync(new ApplyCouponToTeamCartCommand(scenario.TeamCartId, Testing.TestData.DefaultCouponCode)))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }
}
