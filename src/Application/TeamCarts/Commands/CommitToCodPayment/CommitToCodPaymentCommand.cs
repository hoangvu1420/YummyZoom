using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment;

[Authorize]
public sealed record CommitToCodPaymentCommand(
    Guid TeamCartId
) : IRequest<Result<Unit>>;


