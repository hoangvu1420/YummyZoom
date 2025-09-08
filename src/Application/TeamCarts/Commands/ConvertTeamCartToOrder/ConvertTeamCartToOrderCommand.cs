using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.ConvertTeamCartToOrder;

[Authorize]
public sealed record ConvertTeamCartToOrderCommand(
    Guid TeamCartId,
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country,
    string? SpecialInstructions
) : IRequest<Result<ConvertTeamCartToOrderResponse>>;

public sealed record ConvertTeamCartToOrderResponse(
    Guid OrderId
);


