using YummyZoom.Application.Orders.Commands.HandleStripeWebhook;

namespace YummyZoom.Web.Endpoints;

public class StripeWebhooks : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(this);

        // POST /api/stripe-webhooks
        group.MapPost("/", async (HttpRequest request, ISender sender) =>
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
            
            var command = new HandleStripeWebhookCommand(rawJson, stripeSignatureHeader);
            var result = await sender.Send(command);
            
            return result.IsSuccess
                ? Results.Ok()
                : result.ToIResult();
        })
        .WithName("HandleStripeWebhook")
        .WithStandardResults()
        .AllowAnonymous(); // Webhooks don't use standard authentication
    }
}
