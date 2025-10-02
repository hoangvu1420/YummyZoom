using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Orders.Commands.HandleStripeWebhook;
using YummyZoom.Application.TeamCarts.Commands.HandleTeamCartStripeWebhook;

namespace YummyZoom.Web.Endpoints;

public class StripeWebhooks : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(this);

        // POST /api/stripe-webhooks
        group.MapPost("/", async (HttpRequest request, ISender sender, IPaymentGatewayService gateway) =>
        {
            // Read the raw request body
            using var reader = new StreamReader(request.Body);
            var rawJson = await reader.ReadToEndAsync();

            // Get the Stripe signature header
            var stripeSignatureHeader = request.Headers["Stripe-Signature"].FirstOrDefault();

            if (string.IsNullOrEmpty(stripeSignatureHeader))
            {
                return Results.BadRequest("Missing Stripe signature header");
            }

            // Peek into payload to decide routing by metadata (order vs teamcart)
            var constructed = gateway.ConstructWebhookEvent(rawJson, stripeSignatureHeader);
            if (constructed.IsFailure)
            {
                // keep consistent error surface
                return Results.BadRequest(constructed.Error.Description);
            }

            var evt = constructed.Value;
            bool hasTeamCartId = evt.Metadata?.ContainsKey("teamcart_id") == true;
            bool hasOrderId = false;
            if (evt.Metadata is not null && evt.Metadata.TryGetValue("order_id", out var ordVal))
            {
                hasOrderId = !string.IsNullOrWhiteSpace(ordVal);
            }
            bool isTeamCart = hasTeamCartId && !hasOrderId;

            var result = isTeamCart
                ? await sender.Send(new HandleTeamCartStripeWebhookCommand(rawJson, stripeSignatureHeader))
                : await sender.Send(new HandleStripeWebhookCommand(rawJson, stripeSignatureHeader));

            return result.IsSuccess
                ? Results.Ok()
                : result.ToIResult();
        })
        .WithName("HandleStripeWebhook")
        .WithStandardResults()
        .AllowAnonymous(); // Webhooks don't use standard authentication
    }
}
