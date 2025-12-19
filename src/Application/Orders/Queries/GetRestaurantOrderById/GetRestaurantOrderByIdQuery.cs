using FluentValidation;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Orders.Queries.GetRestaurantOrderById;

/// <summary>
/// Retrieves a detailed representation of an Order for a specific restaurant (staff-only).
/// Returns NotFound when the order does not exist or does not belong to the specified restaurant.
/// </summary>
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record GetRestaurantOrderByIdQuery(Guid RestaurantGuid, Guid OrderIdGuid)
    : IRequest<Result<OrderDetailsDto>>, IRestaurantQuery
{
    RestaurantId IRestaurantQuery.RestaurantId => RestaurantId.Create(RestaurantGuid);
}

public static class GetRestaurantOrderByIdErrors
{
    public static Error NotFound => Error.NotFound(
        "GetRestaurantOrderById.NotFound",
        "The requested order was not found.");
}

public sealed class GetRestaurantOrderByIdQueryValidator : AbstractValidator<GetRestaurantOrderByIdQuery>
{
    public GetRestaurantOrderByIdQueryValidator()
    {
        RuleFor(x => x.RestaurantGuid).NotEmpty();
        RuleFor(x => x.OrderIdGuid).NotEmpty();
    }
}

