using MediatR;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.Orders.Queries.GetRestaurantActiveOrders;

/// <summary>
/// Retrieves a paginated list of active orders (Placed, Accepted, Preparing, ReadyForDelivery)
/// for the specified restaurant. Intended for operational dashboards showing work-in-progress.
/// Ordered by status priority then placement timestamp ascending (FIFO within a status) then Id.
/// </summary>
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record GetRestaurantActiveOrdersQuery(Guid RestaurantGuid, int PageNumber, int PageSize)
    : IRequest<Result<PaginatedList<OrderSummaryDto>>>, IRestaurantQuery
{
    RestaurantId IRestaurantQuery.RestaurantId => RestaurantId.Create(RestaurantGuid);
}
