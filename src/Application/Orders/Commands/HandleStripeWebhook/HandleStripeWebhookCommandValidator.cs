using System.Text.Json;

namespace YummyZoom.Application.Orders.Commands.HandleStripeWebhook;

public class HandleStripeWebhookCommandValidator : AbstractValidator<HandleStripeWebhookCommand>
{
    public HandleStripeWebhookCommandValidator()
    {
        RuleFor(x => x.RawJson)
            .NotEmpty()
            .WithMessage("Raw JSON is required.")
            .Must(BeValidJson)
            .WithMessage("Raw JSON must be valid JSON format.");

        RuleFor(x => x.StripeSignatureHeader)
            .NotEmpty()
            .WithMessage("Stripe signature header is required.");
    }

    private static bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
