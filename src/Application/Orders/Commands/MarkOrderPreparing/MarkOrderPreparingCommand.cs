using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Orders.Common;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Orders.Commands.MarkOrderPreparing;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record MarkOrderPreparingCommand(
    Guid OrderId,
    Guid RestaurantGuid
) : IRequest<Result<OrderLifecycleResultDto>>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => RestaurantId.Create(RestaurantGuid);
}
