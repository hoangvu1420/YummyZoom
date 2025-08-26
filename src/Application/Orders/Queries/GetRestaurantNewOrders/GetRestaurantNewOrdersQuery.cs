using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.Orders.Queries.GetRestaurantNewOrders;

/// <summary>
/// Retrieves paginated list of "new" (Status = Placed) orders for a restaurant.
/// Restricted to staff/owner via policy. Ordering is ascending to support FIFO operational processing.
/// </summary>
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record GetRestaurantNewOrdersQuery(Guid RestaurantGuid, int PageNumber, int PageSize)
    : IRequest<Result<PaginatedList<OrderSummaryDto>>>, IRestaurantQuery
{
    RestaurantId IRestaurantQuery.RestaurantId => RestaurantId.Create(RestaurantGuid);
}
