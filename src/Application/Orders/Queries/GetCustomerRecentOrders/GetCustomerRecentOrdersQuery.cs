using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Orders.Queries.GetCustomerRecentOrders;

/// <summary>
/// Retrieves a paginated list of the current authenticated customer's recent orders (most recent first).
/// The customer context is inferred from the current user principal; no explicit CustomerId parameter is accepted
/// to avoid accidental information disclosure. Empty result sets are valid and do not constitute an error.
/// </summary>
public sealed record GetCustomerRecentOrdersQuery(int PageNumber, int PageSize)
    : IRequest<Result<PaginatedList<OrderSummaryDto>>>;
