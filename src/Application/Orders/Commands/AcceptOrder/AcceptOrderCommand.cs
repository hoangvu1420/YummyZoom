using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Orders.Commands.Common;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel.Constants;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Orders.Commands.AcceptOrder;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record AcceptOrderCommand(
    Guid OrderId,
    Guid RestaurantGuid,
    DateTime EstimatedDeliveryTime
) : IRequest<Result<OrderLifecycleResultDto>>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => RestaurantId.Create(RestaurantGuid);
}
