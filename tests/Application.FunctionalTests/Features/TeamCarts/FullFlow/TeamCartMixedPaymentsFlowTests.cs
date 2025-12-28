using Microsoft.Extensions.Options;
using Stripe;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.PaymentIntegration;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment;
using YummyZoom.Application.TeamCarts.Commands.ConvertTeamCartToOrder;
using YummyZoom.Application.TeamCarts.Commands.HandleTeamCartStripeWebhook;
using YummyZoom.Application.TeamCarts.Commands.InitiateMemberOnlinePayment;
using YummyZoom.Application.TeamCarts.Commands.FinalizePricing;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Payments.Stripe;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.FullFlow;

/// <summary>
/// End-to-end flow mixing Online (Stripe) and Cash-on-Delivery member payments,
/// asserting ReadyToConfirm transition and mixed PaymentTransactions upon conversion.
/// </summary>
[Category("StripeIntegration")]
[NonParallelizable]
public class TeamCartMixedPaymentsFlowTests : BaseTestFixture
{
    private StripeOptions _stripeOptions = null!;

    [SetUp]
    public void SetUp()
    {
        _stripeOptions = GetService<IOptions<StripeOptions>>().Value;
        if (string.IsNullOrWhiteSpace(_stripeOptions.SecretKey) || string.IsNullOrWhiteSpace(_stripeOptions.WebhookSecret))
        {
            Assert.Inconclusive("Stripe secrets are not configured for functional tests.");
        }
        StripeConfiguration.ApiKey = _stripeOptions.SecretKey;
    }

    [Test]
    public async Task MixedOnlineAndCOD_AllCommitted_ConvertsWithMixedPaymentTransactions()
    {
        // Arrange: Host + 2 guests
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .WithMultipleGuests("Guest A", "Guest B")
            .BuildAsync();

        await DrainOutboxAsync();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);

        // Each member adds one item
        await scenario.ActAsHost();
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        await scenario.ActAsGuest("Guest A");
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        await scenario.ActAsGuest("Guest B");
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Lock for payment
        await scenario.ActAsHost();
        (await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();
        (await SendAsync(new FinalizePricingCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        // Online payments: Host + Guest A
        var hostUserId = scenario.HostUserId;
        var guestAUserId = scenario.GetGuestUserId("Guest A");
        var guestBUserId = scenario.GetGuestUserId("Guest B");

        await scenario.ActAsHost();
        var hostPayment = await SendAsync(new InitiateMemberOnlinePaymentCommand(scenario.TeamCartId));
        hostPayment.IsSuccess.Should().BeTrue();

        await scenario.ActAsGuest("Guest A");
        var guestAPayment = await SendAsync(new InitiateMemberOnlinePaymentCommand(scenario.TeamCartId));
        guestAPayment.IsSuccess.Should().BeTrue();

        // Guest B commits COD
        await scenario.ActAsGuest("Guest B");
        (await SendAsync(new CommitToCodPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Confirm online payments via Stripe
        await PaymentTestHelper.ConfirmPaymentAsync(hostPayment.Value.PaymentIntentId!, TestConfiguration.Payment.TestPaymentMethods.VisaSuccess);
        await PaymentTestHelper.ConfirmPaymentAsync(guestAPayment.Value.PaymentIntentId!, TestConfiguration.Payment.TestPaymentMethods.VisaSuccess);

        // Process Stripe webhooks for succeeded intents
        async Task ProcessSucceededAsync(string paymentIntentId, Guid memberUserId)
        {
            var payload = PaymentTestHelper.GenerateWebhookPayload(
                TestConfiguration.Payment.WebhookEvents.PaymentIntentSucceeded,
                paymentIntentId,
                amount: 1000,
                currency: "vnd",
                metadata: new Dictionary<string, string>
                {
                    ["source"] = "teamcart",
                    ["teamcart_id"] = scenario.TeamCartId.ToString(),
                    ["member_user_id"] = memberUserId.ToString()
                });

            var signature = PaymentTestHelper.GenerateWebhookSignature(payload, _stripeOptions.WebhookSecret);
            var result = await SendAsync(new HandleTeamCartStripeWebhookCommand(payload, signature));
            result.IsSuccess.Should().BeTrue();
            await DrainOutboxAsync();
        }

        await ProcessSucceededAsync(hostPayment.Value.PaymentIntentId!, hostUserId);
        await ProcessSucceededAsync(guestAPayment.Value.PaymentIntentId!, guestAUserId);

        // Assert: Cart is ReadyToConfirm (all committed: 2 online paid + 1 COD)
        var cart = await Testing.FindTeamCartAsync(TeamCartId.Create(scenario.TeamCartId));
        cart.Should().NotBeNull();
        cart!.Status.Should().Be(TeamCartStatus.ReadyToConfirm);

        // Convert to Order
        await scenario.ActAsHost();
        var convert = await SendAsync(new ConvertTeamCartToOrderCommand(
            scenario.TeamCartId,
            Street: "123 Main St",
            City: "City",
            State: "CA",
            ZipCode: "90210",
            Country: "US",
            SpecialInstructions: "Mix payments"));

        convert.IsSuccess.Should().BeTrue();
        var orderId = OrderId.Create(convert.Value.OrderId);

        var order = await FindOrderAsync(orderId);
        order.Should().NotBeNull();
        order!.SourceTeamCartId!.Value.Should().Be(scenario.TeamCartId);
        order.PaymentTransactions.Should().HaveCount(3);
        order.PaymentTransactions.Count(t => t.PaymentMethodType == PaymentMethodType.CreditCard).Should().Be(2);
        order.PaymentTransactions.Count(t => t.PaymentMethodType == PaymentMethodType.CashOnDelivery).Should().Be(1);

        // Final: TeamCart is Converted
        cart = await Testing.FindTeamCartAsync(TeamCartId.Create(scenario.TeamCartId));
        cart!.Status.Should().Be(TeamCartStatus.Converted);
    }
}
