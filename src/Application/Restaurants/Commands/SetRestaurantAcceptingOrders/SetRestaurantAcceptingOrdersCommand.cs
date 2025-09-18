using FluentValidation;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Restaurants.Commands.SetRestaurantAcceptingOrders;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record SetRestaurantAcceptingOrdersCommand(
    Guid RestaurantId,
    bool IsAccepting
) : IRequest<Result<SetRestaurantAcceptingOrdersResponse>>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed record SetRestaurantAcceptingOrdersResponse(bool IsAccepting);

public sealed class SetRestaurantAcceptingOrdersCommandValidator : AbstractValidator<SetRestaurantAcceptingOrdersCommand>
{
    public SetRestaurantAcceptingOrdersCommandValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        // IsAccepting is a non-nullable bool; no further validation needed.
    }
}

public sealed class SetRestaurantAcceptingOrdersCommandHandler : IRequestHandler<SetRestaurantAcceptingOrdersCommand, Result<SetRestaurantAcceptingOrdersResponse>>
{
    private readonly IRestaurantRepository _restaurants;
    private readonly IUnitOfWork _uow;

    public SetRestaurantAcceptingOrdersCommandHandler(IRestaurantRepository restaurants, IUnitOfWork uow)
    {
        _restaurants = restaurants;
        _uow = uow;
    }

    public async Task<Result<SetRestaurantAcceptingOrdersResponse>> Handle(SetRestaurantAcceptingOrdersCommand request, CancellationToken cancellationToken)
    {
        return await _uow.ExecuteInTransactionAsync(async () =>
        {
            var id = RestaurantId.Create(request.RestaurantId);
            var restaurant = await _restaurants.GetByIdAsync(id, cancellationToken);
            if (restaurant is null)
            {
                return Result.Failure<SetRestaurantAcceptingOrdersResponse>(SetRestaurantAcceptingOrdersErrors.NotFound(request.RestaurantId));
            }

            if (request.IsAccepting)
            {
                restaurant.AcceptOrders();
            }
            else
            {
                restaurant.DeclineOrders();
            }

            await _restaurants.UpdateAsync(restaurant, cancellationToken);

            return Result.Success(new SetRestaurantAcceptingOrdersResponse(restaurant.IsAcceptingOrders));
        }, cancellationToken);
    }
}

public static class SetRestaurantAcceptingOrdersErrors
{
    public static Error NotFound(Guid restaurantId) => Error.NotFound(
        "Restaurant.SetAcceptingOrders.NotFound",
        $"Restaurant '{restaurantId}' was not found.");
}
