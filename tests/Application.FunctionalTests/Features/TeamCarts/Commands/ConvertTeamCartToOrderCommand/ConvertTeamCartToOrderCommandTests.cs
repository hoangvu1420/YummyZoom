using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
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
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var scenario = await TeamCartTestBuilder
            .Create(restaurantId)
            .WithHost("Host")
            .BuildAsync();

        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 2))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        var convert = await SendAsync(new Application.TeamCarts.Commands.ConvertTeamCartToOrder.ConvertTeamCartToOrderCommand(
            scenario.TeamCartId,
            Street: "123 Main St",
            City: "City",
            State: "CA",
            ZipCode: "90210",
            Country: "US",
            SpecialInstructions: "leave at door"));

        convert.IsSuccess.Should().BeTrue();
        convert.Value.OrderId.Should().NotBeEmpty();

        var cart = await Testing.FindTeamCartAsync(TeamCartId.Create(scenario.TeamCartId));
        cart!.Status.Should().Be(TeamCartStatus.Converted);
    }

    [Test]
    public async Task Convert_Should_Fail_WhenPaidSumNotEqualToGrandTotal()
    {
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var scenario = await TeamCartTestBuilder
            .Create(restaurantId)
            .WithHost("Host")
            .WithGuest("Guest")
            .BuildAsync();

        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Do not pay/commit for guest so sum != GrandTotal
        var convert = await SendAsync(new Application.TeamCarts.Commands.ConvertTeamCartToOrder.ConvertTeamCartToOrderCommand(
            scenario.TeamCartId,
            Street: "123 Main St",
            City: "City",
            State: "CA",
            ZipCode: "90210",
            Country: "US",
            SpecialInstructions: "Leave at door"));

        convert.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Convert_WithCoupon_Should_ApplyDiscount_AndSucceed()
    {
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var scenario = await TeamCartTestBuilder
            .Create(restaurantId)
            .WithHost("Host")
            .BuildAsync();

        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 2))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(scenario.TeamCartId, Testing.TestData.DefaultCouponCode))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();
        (await SendAsync(new Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        var convert = await SendAsync(new Application.TeamCarts.Commands.ConvertTeamCartToOrder.ConvertTeamCartToOrderCommand(
            scenario.TeamCartId,
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
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        var limitedCouponCode = await CouponTestDataFactory.CreateTestCouponAsync(new CouponTestOptions
        {
            Code = "LIMITEDUSER1",
            UserUsageLimit = 1,
            TotalUsageLimit = 100,
            DiscountPercentage = 15
        });

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);

        // First cart scenario
        var scenario1 = await TeamCartTestBuilder
            .Create(restaurantId)
            .WithHost("Host")
            .BuildAsync();
        await scenario1.ActAsHost();
        (await SendAsync(new AddItemToTeamCartCommand(scenario1.TeamCartId, burgerId, 2))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario1.TeamCartId))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(scenario1.TeamCartId, limitedCouponCode))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();
        (await SendAsync(new Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommand(scenario1.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();
        var firstConvert = await SendAsync(new Application.TeamCarts.Commands.ConvertTeamCartToOrder.ConvertTeamCartToOrderCommand(
            scenario1.TeamCartId,
            Street: "123 Main St",
            City: "City",
            State: "CA",
            ZipCode: "90210",
            Country: "US",
            SpecialInstructions: "First order"));
        firstConvert.IsSuccess.Should().BeTrue();

        // Second cart scenario (reuse same host user implicitly via helper since host email deterministic?)
        var scenario2 = await TeamCartTestBuilder
            .Create(restaurantId)
            .WithHost("Host")
            .BuildAsync();
        await scenario2.ActAsHost();
        (await SendAsync(new AddItemToTeamCartCommand(scenario2.TeamCartId, burgerId, 2))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario2.TeamCartId))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.ApplyCouponToTeamCart.ApplyCouponToTeamCartCommand(scenario2.TeamCartId, limitedCouponCode))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();
        (await SendAsync(new Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommand(scenario2.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        var secondConvert = await SendAsync(new Application.TeamCarts.Commands.ConvertTeamCartToOrder.ConvertTeamCartToOrderCommand(
            scenario2.TeamCartId,
            Street: "123 Main St",
            City: "City",
            State: "CA",
            ZipCode: "90210",
            Country: "US",
            SpecialInstructions: "Second order"));

        secondConvert.IsFailure.Should().BeTrue();
        secondConvert.Error.Code.Should().Match(x => x == "Coupon.UserUsageLimitExceeded" || x == "Coupon.UsageLimitExceeded");
    }

    [Test]
    public async Task Convert_Should_Fail_WhenNotReadyToConfirm()
    {
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var scenario = await TeamCartTestBuilder
            .Create(restaurantId)
            .WithHost("Host")
            .BuildAsync();
        await scenario.ActAsHost();

        var convert = await SendAsync(new Application.TeamCarts.Commands.ConvertTeamCartToOrder.ConvertTeamCartToOrderCommand(
            scenario.TeamCartId,
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
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var scenario = await TeamCartTestBuilder
            .Create(restaurantId)
            .WithHost("Alice Host")
            .WithGuest("Bob Guest")
            .BuildAsync();

        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Act as guest and attempt convert (should trigger authorization failure)
        await scenario.ActAsGuest("Bob Guest");
        await FluentActions.Invoking(() => SendAsync(new Application.TeamCarts.Commands.ConvertTeamCartToOrder.ConvertTeamCartToOrderCommand(
                scenario.TeamCartId,
                Street: "123 Main St",
                City: "City",
                State: "CA",
                ZipCode: "90210",
                Country: "US",
                SpecialInstructions: null)))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }
}

