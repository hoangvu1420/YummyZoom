using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.InitiateMemberOnlinePayment;

[Authorize]
public sealed record InitiateMemberOnlinePaymentCommand(
    Guid TeamCartId
) : IRequest<Result<InitiateMemberOnlinePaymentResponse>>;

public sealed record InitiateMemberOnlinePaymentResponse(
    string PaymentIntentId,
    string ClientSecret
);
