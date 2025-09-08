using Microsoft.Extensions.Options;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Application.FunctionalTests.Features.Orders.PaymentIntegration;
using YummyZoom.Infrastructure.Payments.Stripe;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.HandleTeamCartStripeWebhookCommand;

public class HandleTeamCartStripeWebhookCommandTests : BaseTestFixture
{
    [Test]
    public async Task Webhook_Succeeded_Should_Record_Payment_And_Succeed()
    {
        var userId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, itemId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        var initiate = await SendAsync(new Application.TeamCarts.Commands.InitiateMemberOnlinePayment.InitiateMemberOnlinePaymentCommand(create.Value.TeamCartId));
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
                ["teamcart_id"] = create.Value.TeamCartId.ToString(),
                ["member_user_id"] = userId.ToString()
            });

        var stripeOptions = GetService<IOptions<StripeOptions>>().Value;
        var signature = PaymentTestHelper.GenerateWebhookSignature(payload, stripeOptions.WebhookSecret);

        var result = await SendAsync(new Application.TeamCarts.Commands.HandleTeamCartStripeWebhook.HandleTeamCartStripeWebhookCommand(payload, signature));
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


