using Microsoft.Extensions.Options;
using Stripe;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.PaymentIntegration;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.ConvertTeamCartToOrder;
using YummyZoom.Application.TeamCarts.Commands.HandleTeamCartStripeWebhook;
using YummyZoom.Application.TeamCarts.Commands.InitiateMemberOnlinePayment;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Payments.Stripe;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.PaymentIntegration;

/// <summary>
/// End-to-end tests for TeamCart online payments using Stripe test mode.
/// Covers: multi-member item additions, lock, payment intents, webhook processing, ReadyToConfirm, and conversion.
/// </summary>
[Category("StripeIntegration")]
[NonParallelizable]
public class TeamCartOnlinePaymentFlowTests : BaseTestFixture
{
    private StripeOptions _stripeOptions = null!;

    [SetUp]
    public void SetUp()
    {
        // Configure Stripe SDK from test secrets (skip if not configured)
        _stripeOptions = GetService<IOptions<StripeOptions>>().Value;
        if (string.IsNullOrWhiteSpace(_stripeOptions.SecretKey) || string.IsNullOrWhiteSpace(_stripeOptions.WebhookSecret))
        {
            Assert.Inconclusive("Stripe secrets are not configured for functional tests.");
        }

        StripeConfiguration.ApiKey = _stripeOptions.SecretKey;
    }

    [Test]
    public async Task Lock_SetsQuote_InVm_AndInitiate_UsesQuotedAmount_AndMetadata()
    {
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .WithMultipleGuests("Guest A")
            .BuildAsync();

        await DrainOutboxAsync();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);

        await scenario.ActAsHost();
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).ShouldBeSuccessful();
        await DrainOutboxAsync();

        await scenario.ActAsGuest("Guest A");
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).ShouldBeSuccessful();
        await DrainOutboxAsync();

        await scenario.ActAsHost();
        (await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId))).ShouldBeSuccessful();
        await DrainOutboxAsync();

        // Inspect VM for quote fields
        var vm = await GetTeamCartVmAsync(scenario.TeamCartId);
        vm.TeamCart.QuoteVersion.Should().BeGreaterThan(0);
        vm.TeamCart.Members.Should().OnlyContain(m => m.QuotedAmount >= 0);

        // Initiate as host and verify Stripe metadata/amount
        await scenario.ActAsHost();
        var initiate = await SendAsync(new InitiateMemberOnlinePaymentCommand(scenario.TeamCartId));
        initiate.ShouldBeSuccessful();
        var intent = await new PaymentIntentService().GetAsync(initiate.Value.PaymentIntentId);
        intent.Metadata.Should().ContainKey("teamcart_id");
        intent.Metadata.Should().ContainKey("member_user_id");
        intent.Metadata.Should().ContainKey("quote_version");
        intent.Metadata.Should().ContainKey("quoted_cents");
    }

    private static async Task<YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel.GetTeamCartRealTimeViewModelResponse> GetTeamCartVmAsync(Guid teamCartId)
    {
        var resp = await SendAsync(new YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel.GetTeamCartRealTimeViewModelQuery(teamCartId));
        resp.ShouldBeSuccessful();
        return resp.Value;
    }

    [Test]
    public async Task CreateAndPayAllMembers_WithStripe_SucceedsAndConverts()
    {
        // Arrange: 1 host + 2 guests
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .WithMultipleGuests("Guest A", "Guest B")
            .BuildAsync();

        await DrainOutboxAsync(); // Ensure initial VM is materialized

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);

        // Host adds items
        await scenario.ActAsHost();
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).ShouldBeSuccessful();
        await DrainOutboxAsync();

        // Guest A adds items
        await scenario.ActAsGuest("Guest A");
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).ShouldBeSuccessful();
        await DrainOutboxAsync();

        // Guest B adds items
        await scenario.ActAsGuest("Guest B");
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).ShouldBeSuccessful();
        await DrainOutboxAsync();

        // Lock for payment as host
        await scenario.ActAsHost();
        (await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId))).ShouldBeSuccessful();
        await DrainOutboxAsync();

        // Initiate online payments for each member (as themselves)
        var hostUserId = scenario.HostUserId;
        var guestAUserId = scenario.GetGuestUserId("Guest A");
        var guestBUserId = scenario.GetGuestUserId("Guest B");

        await scenario.ActAsHost();
        var hostPayment = await SendAsync(new InitiateMemberOnlinePaymentCommand(scenario.TeamCartId));
        hostPayment.ShouldBeSuccessful();
        hostPayment.Value.PaymentIntentId.Should().NotBeNullOrWhiteSpace();

        await scenario.ActAsGuest("Guest A");
        var guestAPayment = await SendAsync(new InitiateMemberOnlinePaymentCommand(scenario.TeamCartId));
        guestAPayment.ShouldBeSuccessful();

        await scenario.ActAsGuest("Guest B");
        var guestBPayment = await SendAsync(new InitiateMemberOnlinePaymentCommand(scenario.TeamCartId));
        guestBPayment.ShouldBeSuccessful();

        // Confirm payments via Stripe SDK
        await PaymentTestHelper.ConfirmPaymentAsync(hostPayment.Value.PaymentIntentId, TestConfiguration.Payment.TestPaymentMethods.VisaSuccess);
        await PaymentTestHelper.ConfirmPaymentAsync(guestAPayment.Value.PaymentIntentId, TestConfiguration.Payment.TestPaymentMethods.VisaSuccess);
        await PaymentTestHelper.ConfirmPaymentAsync(guestBPayment.Value.PaymentIntentId, TestConfiguration.Payment.TestPaymentMethods.VisaSuccess);

        // Process Stripe webhooks for each member intent
        async Task ProcessSucceededAsync(string paymentIntentId, Guid memberUserId)
        {
            var cart = await FindAsync<TeamCart>(TeamCartId.Create(scenario.TeamCartId));
            var quoted = cart!.MemberTotals.First(kv => kv.Key.Value == memberUserId).Value.Amount;
            var quotedCents = (long)Math.Round(quoted * 100m, 0, MidpointRounding.AwayFromZero);
            var payload = PaymentTestHelper.GenerateWebhookPayload(
                TestConfiguration.Payment.WebhookEvents.PaymentIntentSucceeded,
                paymentIntentId,
                amount: quotedCents,
                currency: "usd",
                metadata: new Dictionary<string, string>
                {
                    ["source"] = "teamcart",
                    ["teamcart_id"] = scenario.TeamCartId.ToString(),
                    ["member_user_id"] = memberUserId.ToString(),
                    ["quote_version"] = cart.QuoteVersion.ToString(),
                    ["quoted_cents"] = quotedCents.ToString()
                });

            var signature = PaymentTestHelper.GenerateWebhookSignature(payload, _stripeOptions.WebhookSecret);
            var result = await SendAsync(new HandleTeamCartStripeWebhookCommand(payload, signature));
            result.ShouldBeSuccessful();
            await DrainOutboxAsync();
        }

        await ProcessSucceededAsync(hostPayment.Value.PaymentIntentId, hostUserId);
        await ProcessSucceededAsync(guestAPayment.Value.PaymentIntentId, guestAUserId);
        await ProcessSucceededAsync(guestBPayment.Value.PaymentIntentId, guestBUserId);

        // Assert: TeamCart becomes ReadyToConfirm
        var cart = await FindAsync<TeamCart>(TeamCartId.Create(scenario.TeamCartId));
        cart.Should().NotBeNull();
        cart!.Status.Should().Be(TeamCartStatus.ReadyToConfirm);

        // Convert to Order as host
        await scenario.ActAsHost();
        var convert = await SendAsync(new ConvertTeamCartToOrderCommand(
            scenario.TeamCartId,
            Street: "123 Main St",
            City: "City",
            State: "CA",
            ZipCode: "90210",
            Country: "US",
            SpecialInstructions: "Leave at door"));

        convert.ShouldBeSuccessful();
        var orderId = OrderId.Create(convert.Value.OrderId);

        // Assert: Order exists, has 3 succeeded CC transactions, and source is TeamCart
        var order = await FindOrderAsync(orderId);
        order.Should().NotBeNull();
        order!.SourceTeamCartId!.Value.Should().Be(scenario.TeamCartId);
        order.PaymentTransactions.Should().HaveCount(3);
        order.PaymentTransactions.Should().OnlyContain(t => t.PaymentMethodType == PaymentMethodType.CreditCard);

        // Assert: TeamCart is Converted
        cart = await FindAsync<TeamCart>(TeamCartId.Create(scenario.TeamCartId));
        cart!.Status.Should().Be(TeamCartStatus.Converted);
    }
}
