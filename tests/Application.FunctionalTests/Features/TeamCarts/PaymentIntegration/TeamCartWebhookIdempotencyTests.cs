using FluentAssertions;
using Microsoft.Extensions.Options;
using Stripe;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.PaymentIntegration;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.HandleTeamCartStripeWebhook;
using YummyZoom.Application.TeamCarts.Commands.InitiateMemberOnlinePayment;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Payments.Stripe;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.PaymentIntegration;

/// <summary>
/// Verifies webhook idempotency and no-op behavior for TeamCart payment webhooks.
/// </summary>
[Category("StripeIntegration")]
[NonParallelizable]
public class TeamCartWebhookIdempotencyTests : BaseTestFixture
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
    public async Task PaymentIntentSucceeded_DeliveredTwice_ProcessesOnce()
    {
        // Arrange: Single-member cart (host only)
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();
        await DrainOutboxAsync();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await scenario.ActAsHost();
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        (await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Initiate and confirm online payment for host
        var initiate = await SendAsync(new InitiateMemberOnlinePaymentCommand(scenario.TeamCartId));
        initiate.IsSuccess.Should().BeTrue();
        await PaymentTestHelper.ConfirmPaymentAsync(initiate.Value.PaymentIntentId!, TestConfiguration.Payment.TestPaymentMethods.VisaSuccess);

        // Build a single webhook payload/signature pair and send twice
        var payload = PaymentTestHelper.GenerateWebhookPayload(
            TestConfiguration.Payment.WebhookEvents.PaymentIntentSucceeded,
            initiate.Value.PaymentIntentId!,
            amount: 1000,
            currency: "usd",
            metadata: new Dictionary<string, string>
            {
                ["source"] = "teamcart",
                ["teamcart_id"] = scenario.TeamCartId.ToString(),
                ["member_user_id"] = scenario.HostUserId.ToString()
            });
        var signature = PaymentTestHelper.GenerateWebhookSignature(payload, _stripeOptions.WebhookSecret);

        // First delivery
        var first = await SendAsync(new HandleTeamCartStripeWebhookCommand(payload, signature));
        first.IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        var cart = await FindAsync<TeamCart>(TeamCartId.Create(scenario.TeamCartId));
        cart.Should().NotBeNull();
        cart!.Status.Should().Be(TeamCartStatus.ReadyToConfirm);
        cart.MemberPayments.Should().HaveCount(1);

        // Second delivery (duplicate)
        var second = await SendAsync(new HandleTeamCartStripeWebhookCommand(payload, signature));
        second.IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Assert idempotency: state unchanged, single payment commitment remains
        cart = await FindAsync<TeamCart>(TeamCartId.Create(scenario.TeamCartId));
        cart!.Status.Should().Be(TeamCartStatus.ReadyToConfirm);
        cart.MemberPayments.Should().HaveCount(1);
    }

    [Test]
    public async Task WebhookWithoutTeamCartMetadata_IsNoOp_Succeeds()
    {
        // Arrange a cart but do not depend on it
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();
        await DrainOutboxAsync();

        // Create a webhook without relevant metadata
        var payload = PaymentTestHelper.GenerateWebhookPayload(
            TestConfiguration.Payment.WebhookEvents.PaymentIntentSucceeded,
            $"pi_{Guid.NewGuid():N}");
        var signature = PaymentTestHelper.GenerateWebhookSignature(payload, _stripeOptions.WebhookSecret);

        // Act
        var result = await SendAsync(new HandleTeamCartStripeWebhookCommand(payload, signature));

        // Assert: handler returns success (no-op)
        result.IsSuccess.Should().BeTrue();

        // Cart remains in initial Open state (since we didn't lock or pay)
        var cart = await FindAsync<TeamCart>(TeamCartId.Create(scenario.TeamCartId));
        cart!.Status.Should().Be(TeamCartStatus.Open);
    }
}

