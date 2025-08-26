using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Orders.Queries.GetOrderStatus;

/// <summary>
/// Retrieves the lean status projection for an Order (status + last update + estimated delivery time).
/// Authorization is enforced post-fetch: caller must be either the order's customer or restaurant staff
/// for the order's restaurant. Existence of unauthorized orders is masked as NotFound.
/// </summary>
public sealed record GetOrderStatusQuery(Guid OrderIdGuid) : IRequest<Result<OrderStatusDto>>, IOrderQuery
{
    OrderId IOrderQuery.OrderId => OrderId.Create(OrderIdGuid);
}

/// <summary>
/// Errors specific to the GetOrderStatus query.
/// </summary>
public static class GetOrderStatusErrors
{
    public static Error NotFound => Error.NotFound(
        "GetOrderStatus.NotFound", "The requested order was not found.");
}
