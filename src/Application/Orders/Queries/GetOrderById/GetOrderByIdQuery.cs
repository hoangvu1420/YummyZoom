using YummyZoom.Application.Common.Authorization;
using YummyZoom.SharedKernel;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Application.Orders.Queries.GetOrderById;

/// <summary>
/// Retrieves a detailed representation of an Order including line items and customizations.
/// Authorization is enforced post-fetch: caller must be the order's customer or restaurant staff for the order's restaurant.
/// </summary>
public sealed record GetOrderByIdQuery(Guid OrderIdGuid) : IRequest<Result<GetOrderByIdResponse>>, IOrderQuery
{
    // Map primitive to ValueObject for contextual authorization interface.
    OrderId IOrderQuery.OrderId => OrderId.Create(OrderIdGuid);
}

public sealed record GetOrderByIdResponse(OrderDetailsDto Order);

/// <summary>
/// Errors specific to the GetOrderById query.
/// </summary>
public static class GetOrderByIdErrors
{
    public static Error NotFound => Error.NotFound(
        "GetOrderById.NotFound", "The requested order was not found.");
}
