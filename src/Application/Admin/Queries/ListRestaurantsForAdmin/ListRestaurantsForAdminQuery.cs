using YummyZoom.Application.Common.Models;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Admin.Queries.ListRestaurantsForAdmin;

/// <summary>
/// Retrieves a paginated set of restaurant health summaries for the admin dashboard with optional filtering.
/// </summary>
public sealed record ListRestaurantsForAdminQuery(
    int PageNumber = 1,
    int PageSize = 25,
    bool? IsVerified = null,
    bool? IsAcceptingOrders = null,
    double? MinAverageRating = null,
    int? MinOrdersLast30Days = null,
    decimal? MaxOutstandingBalance = null,
    string? Search = null,
    AdminRestaurantListSort SortBy = AdminRestaurantListSort.RevenueDescending)
    : IRequest<Result<PaginatedList<AdminRestaurantHealthSummaryDto>>>;

/// <summary>
/// Supported sort options for the admin restaurant listing.
/// </summary>
public enum AdminRestaurantListSort
{
    RevenueDescending,
    OrdersDescending,
    RatingDescending,
    OutstandingBalanceDescending,
    OutstandingBalanceAscending,
    LastOrderDescending,
    LastOrderAscending
}
