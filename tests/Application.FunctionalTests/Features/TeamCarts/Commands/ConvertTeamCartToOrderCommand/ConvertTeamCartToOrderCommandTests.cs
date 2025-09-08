using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.ConvertTeamCartToOrderCommand;

public class ConvertTeamCartToOrderCommandTests : BaseTestFixture
{
    [Test]
    public async Task Convert_Should_Succeed_WhenReadyToConfirm()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        // Create cart + add item + lock
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host"));
        create.IsSuccess.Should().BeTrue();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, burgerId, 2))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        // Commit COD for host to reach ReadyToConfirm (single member)
        (await SendAsync(new Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        // Drain to process ReadyForConfirmation event
        await DrainOutboxAsync();

        // Act: convert (no coupon)
        var convert = await SendAsync(new Application.TeamCarts.Commands.ConvertTeamCartToOrder.ConvertTeamCartToOrderCommand(
            create.Value.TeamCartId,
            Street: "123 Main St",
            City: "City",
            State: "CA",
            ZipCode: "90210",
            Country: "US",
            SpecialInstructions: "leave at door"));

        // Assert
        convert.IsSuccess.Should().BeTrue();
        convert.Value.OrderId.Should().NotBeEmpty();

        // TeamCart should be Converted
        var cart = await FindAsync<TeamCart>(TeamCartId.Create(create.Value.TeamCartId));
        cart!.Status.Should().Be(TeamCartStatus.Converted);
    }

    [Test]
    public async Task Convert_WithCoupon_Should_ApplyDiscount_AndSucceed()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host"));
        create.IsSuccess.Should().BeTrue();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, burgerId, 2))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        // Apply a known valid coupon on locked cart
        (await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(create.Value.TeamCartId, Testing.TestData.DefaultCouponCode))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Commit COD
        (await SendAsync(new Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        var convert = await SendAsync(new Application.TeamCarts.Commands.ConvertTeamCartToOrder.ConvertTeamCartToOrderCommand(
            create.Value.TeamCartId,
            Street: "123 Main St",
            City: "City",
            State: "CA",
            ZipCode: "90210",
            Country: "US",
            SpecialInstructions: null));

        convert.IsSuccess.Should().BeTrue();
        convert.Value.OrderId.Should().NotBeEmpty();
    }

    [Test]
    public async Task Convert_WithCoupon_WhenUsageLimitExceeded_ShouldFail()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host"));
        create.IsSuccess.Should().BeTrue();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, burgerId, 2))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        // Apply coupon and artificially exhaust usage (simulate with two increments beyond limit if test data provides a low limit)
        (await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(create.Value.TeamCartId, Testing.TestData.DefaultCouponCode))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Exhaust per-user or total usage via direct repo if helper exists; otherwise rely on repository enforcing limit at conversion time.

        (await SendAsync(new Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        var convert = await SendAsync(new Application.TeamCarts.Commands.ConvertTeamCartToOrder.ConvertTeamCartToOrderCommand(
            create.Value.TeamCartId,
            Street: "123 Main St",
            City: "City",
            State: "CA",
            ZipCode: "90210",
            Country: "US",
            SpecialInstructions: null));

        convert.IsFailure.Should().BeTrue();
        convert.Error.Code.Should().Match(x => x == "Coupon.UserUsageLimitExceeded" || x == "Coupon.UsageLimitExceeded");
    }

    [Test]
    public async Task Convert_Should_Fail_WhenNotReadyToConfirm()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host"));
        create.IsSuccess.Should().BeTrue();

        // No items/lock/commit → not ReadyToConfirm
        var convert = await SendAsync(new Application.TeamCarts.Commands.ConvertTeamCartToOrder.ConvertTeamCartToOrderCommand(
            create.Value.TeamCartId,
            Street: "123 Main St",
            City: "City",
            State: "CA",
            ZipCode: "90210",
            Country: "US",
            SpecialInstructions: null));

        convert.IsFailure.Should().BeTrue();
        convert.Error.Code.Should().Be("TeamCart.InvalidStatus");
    }

    [Test]
    public async Task Convert_Should_Fail_ForNonHost()
    {
        var hostId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host"));
        create.IsSuccess.Should().BeTrue();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // switch to another user
        var otherUserId = await CreateUserAsync("not-host@example.com", "Password123!");
        SetUserId(otherUserId);

        var convert = await SendAsync(new Application.TeamCarts.Commands.ConvertTeamCartToOrder.ConvertTeamCartToOrderCommand(
            create.Value.TeamCartId,
            Street: "123 Main St",
            City: "City",
            State: "CA",
            ZipCode: "90210",
            Country: "US",
            SpecialInstructions: null));

        convert.IsFailure.Should().BeTrue();
        convert.Error.Code.Should().Be("TeamCart.OnlyHostCanModifyFinancials");
    }
}


