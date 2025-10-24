using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.RemoveCouponFromTeamCartCommand;

public class RemoveCouponFromTeamCartCommandTests : BaseTestFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
    }

    [Test]
    public async Task RemoveCoupon_Should_Succeed_ForHost_WhenCartLocked_AndCouponApplied()
    {
        // Arrange: Create team cart scenario with host
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Add item, lock cart, and apply coupon as host
        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions());
        (await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(scenario.TeamCartId, couponCode))).IsSuccess.Should().BeTrue();

        // Act: Remove coupon as host
        var remove = await SendAsync(new Application.TeamCarts.Commands.RemoveCouponFromTeamCart.RemoveCouponFromTeamCartCommand(scenario.TeamCartId));

        // Assert
        remove.IsSuccess.Should().BeTrue();

        var cart = await Testing.FindTeamCartAsync(TeamCartId.Create(scenario.TeamCartId));
        cart.Should().NotBeNull();
        cart!.AppliedCouponId.Should().BeNull();
    }

    [Test]
    public async Task RemoveCoupon_Should_Succeed_NoCouponApplied()
    {
        // Arrange: Create team cart scenario with host (no coupon applied)
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Add item and lock cart as host without applying coupon
        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        // Act: Remove coupon as host (should succeed even when no coupon is applied)
        var remove = await SendAsync(new Application.TeamCarts.Commands.RemoveCouponFromTeamCart.RemoveCouponFromTeamCartCommand(scenario.TeamCartId));

        // Assert
        remove.IsSuccess.Should().BeTrue();
        var cart = await Testing.FindTeamCartAsync(TeamCartId.Create(scenario.TeamCartId));
        cart.Should().NotBeNull();
        cart!.AppliedCouponId.Should().BeNull();
    }

    [Test]
    public async Task RemoveCoupon_Should_Fail_WhenCartOpen()
    {
        // Arrange: Create team cart scenario with host (cart remains open)
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Act: Try to remove coupon from open cart as host
        await scenario.ActAsHost();
        var remove = await SendAsync(new Application.TeamCarts.Commands.RemoveCouponFromTeamCart.RemoveCouponFromTeamCartCommand(scenario.TeamCartId));

        // Assert
        remove.IsFailure.Should().BeTrue();
        remove.Error.Code.Should().Be("TeamCart.CanOnlyApplyFinancialsToLockedCart");
    }

    [Test]
    public async Task RemoveCoupon_Should_Fail_ForNonHost_WhenCartLocked()
    {
        // Arrange: Create team cart scenario with host and guest
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .WithGuest("Guest User")
            .BuildAsync();

        // Add item, lock cart, and apply coupon as host
        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions());
        (await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(scenario.TeamCartId, couponCode))).IsSuccess.Should().BeTrue();

        // Act: Try to remove coupon as guest (non-host) - authorization should fail at pipeline level
        await scenario.ActAsGuest("Guest User");

        // Assert: Should throw ForbiddenAccessException due to authorization policy
        await FluentActions.Invoking(() =>
                SendAsync(new Application.TeamCarts.Commands.RemoveCouponFromTeamCart.RemoveCouponFromTeamCartCommand(scenario.TeamCartId)))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task RemoveCoupon_Should_Fail_ForNonMember()
    {
        // Arrange: Create team cart scenario with host
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Add item, lock cart, and apply coupon as host
        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions());
        (await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(scenario.TeamCartId, couponCode))).IsSuccess.Should().BeTrue();

        // Act: Try to remove coupon as non-member - authorization should fail at pipeline level
        await scenario.ActAsNonMember();

        // Assert: Should throw ForbiddenAccessException due to authorization policy
        await FluentActions.Invoking(() =>
                SendAsync(new Application.TeamCarts.Commands.RemoveCouponFromTeamCart.RemoveCouponFromTeamCartCommand(scenario.TeamCartId)))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }
}

