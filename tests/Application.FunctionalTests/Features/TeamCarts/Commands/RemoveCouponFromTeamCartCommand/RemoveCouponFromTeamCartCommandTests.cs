using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.RemoveCouponFromTeamCartCommand;

public class RemoveCouponFromTeamCartCommandTests : BaseTestFixture
{
    [Test]
    public async Task RemoveCoupon_Should_Succeed_ForHost_WhenCartLocked_AndCouponApplied()
    {
        // Arrange: host creates cart, adds item, locks, applies coupon
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions());
        (await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(create.Value.TeamCartId, couponCode))).IsSuccess.Should().BeTrue();

        // Act
        var remove = await SendAsync(new Application.TeamCarts.Commands.RemoveCouponFromTeamCart.RemoveCouponFromTeamCartCommand(create.Value.TeamCartId));

        // Assert
        remove.IsSuccess.Should().BeTrue();

        using var scope = TestInfrastructure.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cart = await db.TeamCarts.FirstOrDefaultAsync(c => c.Id == TeamCartId.Create(create.Value.TeamCartId));
        cart.Should().NotBeNull();
        cart!.AppliedCouponId.Should().BeNull();
    }

    [Test]
    public async Task RemoveCoupon_Should_Succeed_NoCouponApplied()
    {
        // Arrange: host creates locked cart without applying coupon
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        // Act
        var remove = await SendAsync(new Application.TeamCarts.Commands.RemoveCouponFromTeamCart.RemoveCouponFromTeamCartCommand(create.Value.TeamCartId));

        // Assert
        remove.IsSuccess.Should().BeTrue();
        var cart = await FindAsync<TeamCart>(TeamCartId.Create(create.Value.TeamCartId));
        cart.Should().NotBeNull();
        cart!.AppliedCouponId.Should().BeNull();
    }

    [Test]
    public async Task RemoveCoupon_Should_Fail_WhenCartOpen()
    {
        // Arrange: host creates cart (open)
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        // Act
        var remove = await SendAsync(new Application.TeamCarts.Commands.RemoveCouponFromTeamCart.RemoveCouponFromTeamCartCommand(create.Value.TeamCartId));

        // Assert
        remove.IsFailure.Should().BeTrue();
        remove.Error.Code.Should().Be("TeamCart.CanOnlyApplyFinancialsToLockedCart");
    }

    [Test]
    public async Task RemoveCoupon_Should_Fail_ForNonHost_WhenCartLocked()
    {
        // Arrange: host creates locked cart with a coupon
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions());
        (await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(create.Value.TeamCartId, couponCode))).IsSuccess.Should().BeTrue();

        // Switch to non-host
        var otherUserId = await CreateUserAsync("not-host-removecoupon@example.com", "Password123!");
        SetUserId(otherUserId);

        // Act
        var remove = await SendAsync(new Application.TeamCarts.Commands.RemoveCouponFromTeamCart.RemoveCouponFromTeamCartCommand(create.Value.TeamCartId));

        // Assert
        remove.IsFailure.Should().BeTrue();
        remove.Error.Code.Should().Be("TeamCart.OnlyHostCanModifyFinancials");
    }
}

