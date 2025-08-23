using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Orders.Commands.Common;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Orders.Commands.MarkOrderReadyForDelivery;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record MarkOrderReadyForDeliveryCommand(
    Guid OrderId,
    Guid RestaurantGuid
) : IRequest<Result<OrderLifecycleResultDto>>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => RestaurantId.Create(RestaurantGuid);
}
