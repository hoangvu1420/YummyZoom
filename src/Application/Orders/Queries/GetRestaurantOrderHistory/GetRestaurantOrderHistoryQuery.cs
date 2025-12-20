using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Orders.Queries.GetRestaurantOrderHistory;

/// <summary>
/// Retrieves a paginated list of historical orders for a restaurant with optional filters.
/// History includes terminal statuses (Delivered, Cancelled, Rejected).
/// </summary>
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record GetRestaurantOrderHistoryQuery(
    Guid RestaurantGuid,
    int PageNumber,
    int PageSize,
    DateTime? From = null,
    DateTime? To = null,
    string? Statuses = null,
    string? Keyword = null)
    : IRequest<Result<PaginatedList<OrderHistorySummaryDto>>>, IRestaurantQuery
{
    RestaurantId IRestaurantQuery.RestaurantId => RestaurantId.Create(RestaurantGuid);
}
