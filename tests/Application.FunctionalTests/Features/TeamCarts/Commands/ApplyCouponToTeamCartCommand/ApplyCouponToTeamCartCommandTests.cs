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
using YummyZoom.Infrastructure.Data;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.ApplyCouponToTeamCartCommand;

public class ApplyCouponToTeamCartCommandTests : BaseTestFixture
{
    [Test]
    public async Task ApplyCoupon_Should_Succeed_ForHost_WhenCartLocked()
    {
        // Arrange: host creates cart, adds an item, locks for payment
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, itemId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        // Create a valid coupon for the default restaurant
        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions());

        // Act
        var apply = await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(create.Value.TeamCartId, couponCode));

        // Assert: success and persisted AppliedCouponId set to the coupon's ID
        apply.IsSuccess.Should().BeTrue();

        using var scope = TestInfrastructure.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cart = await db.TeamCarts.FirstOrDefaultAsync(c => c.Id == TeamCartId.Create(create.Value.TeamCartId));
        cart.Should().NotBeNull();
        cart!.AppliedCouponId.Should().NotBeNull();

        var coupon = await db.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode);
        coupon.Should().NotBeNull();
        cart!.AppliedCouponId!.Value.Should().Be(coupon!.Id.Value);
    }

    [Test]
    public async Task ApplyCoupon_Should_Fail_WhenCartOpen()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions());
        var apply = await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(create.Value.TeamCartId, couponCode));

        apply.IsFailure.Should().BeTrue();
        apply.Error.Code.Should().Be("TeamCart.CanOnlyApplyFinancialsToLockedCart");
    }

    [Test]
    public async Task ApplyCoupon_Should_Fail_ForNonHost_WhenCartLocked()
    {
        // Arrange locked cart by host
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, itemId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions());

        // Switch to non-host
        var otherUserId = await CreateUserAsync("not-host-applycoupon@example.com", "Password123!");
        SetUserId(otherUserId);

        // Act
        var apply = await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(create.Value.TeamCartId, couponCode));

        // Assert
        apply.IsFailure.Should().BeTrue();
        apply.Error.Code.Should().Be("TeamCart.OnlyHostCanModifyFinancials");
    }

    [Test]
    public async Task ApplyCoupon_Should_Fail_WhenCouponNotFound()
    {
        // Arrange locked cart
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, itemId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        // Act
        var apply = await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(create.Value.TeamCartId, "NO_SUCH_CODE"));

        // Assert
        apply.IsFailure.Should().BeTrue();
        apply.Error.Code.Should().Be("Coupon.CouponNotFound");
    }

    [Test]
    public async Task ApplyCoupon_Should_Fail_WhenCouponNotApplicableToCartItems()
    {
        // Arrange locked cart with a burger item
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        // Create a coupon that applies to a different item (e.g., Margherita Pizza)
        var pizzaId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.MargheritaPizza);
        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions
        {
            SpecificMenuItemId = pizzaId
        });

        // Act
        var apply = await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(create.Value.TeamCartId, couponCode));

        // Assert: NotApplicable error
        apply.IsFailure.Should().BeTrue();
        apply.Error.Code.Should().Be("Coupon.NotApplicable");
    }

    [Test]
    public async Task ApplyCoupon_Should_Fail_WhenMinimumOrderAmountNotMet()
    {
        // Arrange locked cart with a single, small item
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        // Create a coupon with a high minimum order amount to force failure
        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions
        {
            MinimumOrderAmount = 999m
        });

        // Act
        var apply = await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(create.Value.TeamCartId, couponCode));

        // Assert: MinAmountNotMet
        apply.IsFailure.Should().BeTrue();
        apply.Error.Code.Should().Be("Coupon.MinAmountNotMet");
    }

    [Test]
    public async Task ApplyCoupon_WhitespaceCode_ShouldFailValidation()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        await FluentActions.Invoking(() =>
                SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(create.Value.TeamCartId, "   ")))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ValidationException>();
    }
}
