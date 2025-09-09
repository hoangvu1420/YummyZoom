using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.ApplyCouponToTeamCartCommand;

public class ApplyCouponToTeamCartCommandTests : BaseTestFixture
{
    [Test]
    public async Task ApplyCoupon_Should_Succeed_ForHost_WhenCartLocked()
    {
        // Arrange: Create team cart scenario, add item, and lock for payment
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        // Create a valid coupon for the default restaurant
        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions());

        // Act: Apply coupon as host
        var apply = await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(scenario.TeamCartId, couponCode));

        // Assert: success and persisted AppliedCouponId set to the coupon's ID
        apply.IsSuccess.Should().BeTrue();

        using var scope = TestInfrastructure.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cart = await db.TeamCarts.FirstOrDefaultAsync(c => c.Id == TeamCartId.Create(scenario.TeamCartId));
        cart.Should().NotBeNull();
        cart!.AppliedCouponId.Should().NotBeNull();

        var coupon = await db.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode);
        coupon.Should().NotBeNull();
        cart!.AppliedCouponId!.Value.Should().Be(coupon!.Id.Value);
    }

    [Test]
    public async Task ApplyCoupon_Should_Fail_WhenCartOpen()
    {
        // Arrange: Create team cart scenario (cart remains open, not locked)
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Act: Try to apply coupon to open cart as host
        await scenario.ActAsHost();
        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions());
        var apply = await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(scenario.TeamCartId, couponCode));

        // Assert: Should fail because cart is not locked
        apply.IsFailure.Should().BeTrue();
        apply.Error.Code.Should().Be("TeamCart.CanOnlyApplyFinancialsToLockedCart");
    }

    [Test]
    public async Task ApplyCoupon_Should_Fail_ForNonHost_WhenCartLocked()
    {
        // Arrange: Create locked cart scenario
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions());

        // Act & Assert: Switch to non-host and try to apply coupon should throw ForbiddenAccessException
        var otherUserId = await CreateUserAsync("not-host-applycoupon@example.com", "Password123!");
        SetUserId(otherUserId);
        
        await FluentActions.Invoking(() => 
                SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(scenario.TeamCartId, couponCode)))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ForbiddenAccessException>();
    }

    [Test]
    public async Task ApplyCoupon_Should_Fail_WhenCouponNotFound()
    {
        // Arrange: Create locked cart scenario
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        // Act: Try to apply non-existent coupon as host
        var apply = await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(scenario.TeamCartId, "NO_SUCH_CODE"));

        // Assert: Should fail because coupon doesn't exist
        apply.IsFailure.Should().BeTrue();
        apply.Error.Code.Should().Be("Coupon.CouponNotFound");
    }

    [Test]
    public async Task ApplyCoupon_Should_Fail_WhenCouponNotApplicableToCartItems()
    {
        // Arrange: Create locked cart scenario with burger item
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        // Create a coupon that applies to a different item (e.g., Margherita Pizza)
        var pizzaId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.MargheritaPizza);
        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions
        {
            SpecificMenuItemId = pizzaId
        });

        // Act: Try to apply non-applicable coupon as host
        var apply = await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(scenario.TeamCartId, couponCode));

        // Assert: Should fail because coupon is not applicable to cart items
        apply.IsFailure.Should().BeTrue();
        apply.Error.Code.Should().Be("Coupon.NotApplicable");
    }

    [Test]
    public async Task ApplyCoupon_Should_Fail_WhenMinimumOrderAmountNotMet()
    {
        // Arrange: Create locked cart scenario with single item
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        // Create a coupon with a high minimum order amount to force failure
        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions
        {
            MinimumOrderAmount = 999m
        });

        // Act: Try to apply coupon with unmet minimum amount as host
        var apply = await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(scenario.TeamCartId, couponCode));

        // Assert: Should fail because minimum order amount not met
        apply.IsFailure.Should().BeTrue();
        apply.Error.Code.Should().Be("Coupon.MinAmountNotMet");
    }

    [Test]
    public async Task ApplyCoupon_WhitespaceCode_ShouldFailValidation()
    {
        // Arrange: Create team cart scenario
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Act & Assert: Try to apply whitespace coupon code should fail validation
        await scenario.ActAsHost();
        await FluentActions.Invoking(() =>
                SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(scenario.TeamCartId, "   ")))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ValidationException>();
    }
}
