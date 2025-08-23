using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Orders.Commands.Common;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Orders.Commands.CancelOrder;

[Authorize]
public sealed record CancelOrderCommand(
    Guid OrderId,
    Guid RestaurantGuid,
    Guid? ActingUserId,
    string? Reason
) : IRequest<Result<OrderLifecycleResultDto>>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => RestaurantId.Create(RestaurantGuid);
    public UserId? ActingUserDomainId => ActingUserId.HasValue ? UserId.Create(ActingUserId.Value) : null;
}
