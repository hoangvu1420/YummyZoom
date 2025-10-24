using FluentAssertions;
using Microsoft.Extensions.Options;
using Stripe;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.PaymentIntegration;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.ConvertTeamCartToOrder;
using YummyZoom.Application.TeamCarts.Commands.ExpireTeamCarts;
using YummyZoom.Application.TeamCarts.Commands.HandleTeamCartStripeWebhook;
using YummyZoom.Application.TeamCarts.Commands.InitiateMemberOnlinePayment;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Payments.Stripe;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.FullFlow;

public class TeamCartExpirationFlowTests : BaseTestFixture
{
    [Test]
    public async Task UnpaidLockedCart_Expired_BlocksConversion()
    {
        // Arrange: Create and lock a cart with one item, no payments
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();
        await DrainOutboxAsync();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await scenario.ActAsHost();
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();
        (await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        // Act: Force expiration by using a far-future cutoff
        var cutoff = DateTime.UtcNow.AddYears(5);
        var expiredCount = await SendAsync(new ExpireTeamCartsCommand(cutoff, BatchSize: 100));
        expiredCount.Should().BeGreaterOrEqualTo(1);

        await DrainOutboxAsync();

        // Assert: Cart is Expired
        var cart = await Testing.FindTeamCartAsync(TeamCartId.Create(scenario.TeamCartId));
        cart!.Status.Should().Be(TeamCartStatus.Expired);

        // Attempt conversion should fail (invalid status)
        await scenario.ActAsHost();
        var convert = await SendAsync(new ConvertTeamCartToOrderCommand(
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
    [Category("StripeIntegration")]
    [NonParallelizable]
    public async Task ExpiredCart_ReceivingPaymentWebhook_ShouldReturnFailure_AndRemainExpired()
    {
        // Arrange Stripe test setup
        var stripeOptions = GetService<IOptions<StripeOptions>>().Value;
        if (string.IsNullOrWhiteSpace(stripeOptions.SecretKey) || string.IsNullOrWhiteSpace(stripeOptions.WebhookSecret))
        {
            Assert.Inconclusive("Stripe secrets are not configured for functional tests.");
        }
        StripeConfiguration.ApiKey = stripeOptions.SecretKey;

        // Create locked cart and initiate payment but do not process webhook yet
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();
        await DrainOutboxAsync();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await scenario.ActAsHost();
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();
        (await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        var initiate = await SendAsync(new InitiateMemberOnlinePaymentCommand(scenario.TeamCartId));
        initiate.IsSuccess.Should().BeTrue();
        var paymentIntentId = initiate.Value.PaymentIntentId!;

        // Confirm payment on Stripe (external) but do not deliver webhook yet
        await PaymentTestHelper.ConfirmPaymentAsync(paymentIntentId, TestConfiguration.Payment.TestPaymentMethods.VisaSuccess);

        // Expire the cart
        var cutoff = DateTime.UtcNow.AddYears(5);
        var expiredCount = await SendAsync(new ExpireTeamCartsCommand(cutoff, BatchSize: 100));
        expiredCount.Should().BeGreaterOrEqualTo(1);
        await DrainOutboxAsync();

        // Deliver webhook after expiration
        var payload = PaymentTestHelper.GenerateWebhookPayload(
            TestConfiguration.Payment.WebhookEvents.PaymentIntentSucceeded,
            paymentIntentId,
            amount: 1000,
            currency: "vnd",
            metadata: new Dictionary<string, string>
            {
                ["source"] = "teamcart",
                ["teamcart_id"] = scenario.TeamCartId.ToString(),
                ["member_user_id"] = scenario.HostUserId.ToString()
            });
        var signature = PaymentTestHelper.GenerateWebhookSignature(payload, stripeOptions.WebhookSecret);
        var result = await SendAsync(new HandleTeamCartStripeWebhookCommand(payload, signature));

        // Assert: handler returns failure due to non-locked status; cart remains expired
        result.IsFailure.Should().BeTrue();
        var cart = await Testing.FindTeamCartAsync(TeamCartId.Create(scenario.TeamCartId));
        cart!.Status.Should().Be(TeamCartStatus.Expired);
    }
}

