using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.HandleTeamCartStripeWebhook;

public sealed record HandleTeamCartStripeWebhookCommand(
    string RawJson,
    string StripeSignatureHeader
) : IRequest<Result>;
