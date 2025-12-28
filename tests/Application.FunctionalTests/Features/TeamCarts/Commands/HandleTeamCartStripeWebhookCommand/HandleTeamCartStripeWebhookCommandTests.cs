using FluentAssertions;
using Microsoft.Extensions.Options;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.PaymentIntegration;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Infrastructure.Payments.Stripe;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.HandleTeamCartStripeWebhookCommand;

public class HandleTeamCartStripeWebhookCommandTests : BaseTestFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
    }

    [Test]
    public async Task Webhook_Succeeded_Should_Record_Payment_And_Succeed()
    {
        // Arrange: Create team cart scenario with host using builder
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        await DrainOutboxAsync(); // process TeamCartCreated -> create VM

        // Add item and lock cart (as host)
        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.FinalizePricing.FinalizePricingCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        // Initiate payment (as host)
        var initiate = await SendAsync(new Application.TeamCarts.Commands.InitiateMemberOnlinePayment.InitiateMemberOnlinePaymentCommand(scenario.TeamCartId));
        initiate.IsSuccess.Should().BeTrue();

        // Build a fake webhook event payload through ConstructWebhookEvent by crafting raw json metadata
        var payload = PaymentTestHelper.GenerateWebhookPayload(
            "payment_intent.succeeded",
            initiate.Value.PaymentIntentId,
            amount: 1000,
            currency: "usd",
            metadata: new Dictionary<string, string>
            {
                ["source"] = "teamcart",
                ["teamcart_id"] = scenario.TeamCartId.ToString(),
                ["member_user_id"] = scenario.HostUserId.ToString()
            });

        var stripeOptions = GetService<IOptions<StripeOptions>>().Value;
        var signature = PaymentTestHelper.GenerateWebhookSignature(payload, stripeOptions.WebhookSecret);

        // Act: Handle webhook
        var result = await SendAsync(new Application.TeamCarts.Commands.HandleTeamCartStripeWebhook.HandleTeamCartStripeWebhookCommand(payload, signature));

        // Assert: Webhook handling succeeded
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Webhook_With_No_TeamCart_Metadata_Should_NoOp()
    {
        var payload = PaymentTestHelper.GenerateWebhookPayload(
            "payment_intent.succeeded",
            $"pi_{Guid.NewGuid():N}");

        var stripeOptions = GetService<IOptions<StripeOptions>>().Value;
        var signature = PaymentTestHelper.GenerateWebhookSignature(payload, stripeOptions.WebhookSecret);

        var result = await SendAsync(new Application.TeamCarts.Commands.HandleTeamCartStripeWebhook.HandleTeamCartStripeWebhookCommand(payload, signature));
        result.IsSuccess.Should().BeTrue();
    }
}

