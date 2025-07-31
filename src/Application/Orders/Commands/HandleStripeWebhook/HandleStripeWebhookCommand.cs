using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Orders.Commands.HandleStripeWebhook;

public record HandleStripeWebhookCommand(
    string RawJson,
    string StripeSignatureHeader
) : IRequest<Result>;