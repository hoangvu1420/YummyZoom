using Microsoft.Extensions.Options;
using Stripe;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.PaymentIntegration;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment;
using YummyZoom.Application.TeamCarts.Commands.FinalizePricing;
using YummyZoom.Application.TeamCarts.Commands.HandleTeamCartStripeWebhook;
using YummyZoom.Application.TeamCarts.Commands.InitiateMemberOnlinePayment;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
using YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Payments.Stripe;
using static YummyZoom.Application.FunctionalTests.Testing;
using TeamCartPaymentMethod = YummyZoom.Domain.TeamCartAggregate.Enums.PaymentMethod;
using TeamCartPaymentStatus = YummyZoom.Domain.TeamCartAggregate.Enums.PaymentStatus;
using TeamCartStatus = YummyZoom.Domain.TeamCartAggregate.Enums.TeamCartStatus;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.PaymentIntegration;

/// <summary>
/// Tests for failed online payments and member retry UX in TeamCart.
/// </summary>
[Category("StripeIntegration")]
[NonParallelizable]
public class TeamCartOnlinePaymentFailureTests : BaseTestFixture
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
    public async Task MemberPayment_Failed_Webhook_UpdatesVmAndAllowsRetry()
    {
        // Arrange: Host + Guest A with items
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .WithGuest("Guest A")
            .BuildAsync();
        await DrainOutboxAsync();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);

        await scenario.ActAsHost();
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).ShouldBeSuccessful();
        await DrainOutboxAsync();

        await scenario.ActAsGuest("Guest A");
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).ShouldBeSuccessful();
        await DrainOutboxAsync();

        // Lock for payment
        await scenario.ActAsHost();
        (await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId))).ShouldBeSuccessful();
        await DrainOutboxAsync();
        (await SendAsync(new FinalizePricingCommand(scenario.TeamCartId))).ShouldBeSuccessful();

        // Initiate online payment as Guest A
        await scenario.ActAsGuest("Guest A");
        var initiate = await SendAsync(new InitiateMemberOnlinePaymentCommand(scenario.TeamCartId));
        initiate.ShouldBeSuccessful();

        // Confirm payment with a declined card
        await PaymentTestHelper.ConfirmPaymentAsync(initiate.Value.PaymentIntentId!, TestConfiguration.Payment.TestPaymentMethods.VisaDeclined);

        // Build failed webhook and send
        var payload = PaymentTestHelper.GenerateWebhookPayload(
            TestConfiguration.Payment.WebhookEvents.PaymentIntentPaymentFailed,
            initiate.Value.PaymentIntentId!,
            amount: 1000,
            currency: "vnd",
            metadata: new Dictionary<string, string>
            {
                ["source"] = "teamcart",
                ["teamcart_id"] = scenario.TeamCartId.ToString(),
                ["member_user_id"] = scenario.GetGuestUserId("Guest A").ToString()
            });
        var signature = PaymentTestHelper.GenerateWebhookSignature(payload, _stripeOptions.WebhookSecret);

        var result = await SendAsync(new HandleTeamCartStripeWebhookCommand(payload, signature));
        result.ShouldBeSuccessful();
        await DrainOutboxAsync();

        // Assert: domain records a payment entry and remains Finalized
        var cart = await Testing.FindTeamCartAsync(TeamCartId.Create(scenario.TeamCartId));
        cart.Should().NotBeNull();
        cart!.Status.Should().Be(TeamCartStatus.Finalized);
        cart.MemberPayments.Should().NotBeEmpty();
        cart.MemberPayments.Should().Contain(p => p.UserId.Value == scenario.GetGuestUserId("Guest A") && p.Method == TeamCartPaymentMethod.Online && p.Status == TeamCartPaymentStatus.Failed);

        // Assert: VM reflects failed status for Guest A
        await scenario.ActAsGuest("Guest A");
        var vmResp = await SendAsync(new GetTeamCartRealTimeViewModelQuery(scenario.TeamCartId));
        vmResp.ShouldBeSuccessful();
        vmResp.Value.TeamCart.Members.First(m => m.UserId == scenario.GetGuestUserId("Guest A")).PaymentStatus.Should().Be("Failed");

        // Retry: initiate again and succeed
        var retry = await SendAsync(new InitiateMemberOnlinePaymentCommand(scenario.TeamCartId));
        retry.ShouldBeSuccessful();
        await PaymentTestHelper.ConfirmPaymentAsync(retry.Value.PaymentIntentId!, TestConfiguration.Payment.TestPaymentMethods.VisaSuccess);

        var payload2 = PaymentTestHelper.GenerateWebhookPayload(
            TestConfiguration.Payment.WebhookEvents.PaymentIntentSucceeded,
            retry.Value.PaymentIntentId!,
            amount: 1000,
            currency: "vnd",
            metadata: new Dictionary<string, string>
            {
                ["source"] = "teamcart",
                ["teamcart_id"] = scenario.TeamCartId.ToString(),
                ["member_user_id"] = scenario.GetGuestUserId("Guest A").ToString()
            });
        var signature2 = PaymentTestHelper.GenerateWebhookSignature(payload2, _stripeOptions.WebhookSecret);
        var result2 = await SendAsync(new HandleTeamCartStripeWebhookCommand(payload2, signature2));
        result2.ShouldBeSuccessful();
        await DrainOutboxAsync();

        // Host commits COD to reach ReadyToConfirm
        await scenario.ActAsHost();
        (await SendAsync(new CommitToCodPaymentCommand(scenario.TeamCartId))).ShouldBeSuccessful();
        await DrainOutboxAsync();

        cart = await Testing.FindTeamCartAsync(TeamCartId.Create(scenario.TeamCartId));
        cart!.Status.Should().Be(TeamCartStatus.ReadyToConfirm);
        cart.MemberPayments.Should().Contain(p => p.UserId.Value == scenario.GetGuestUserId("Guest A") && p.Status == TeamCartPaymentStatus.PaidOnline);
    }
}
