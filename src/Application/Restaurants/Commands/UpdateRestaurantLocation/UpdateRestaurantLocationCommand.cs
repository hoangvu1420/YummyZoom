using FluentValidation;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Restaurants.Commands.UpdateRestaurantLocation;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record UpdateRestaurantLocationCommand(
    Guid RestaurantId,
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country,
    double? Latitude,
    double? Longitude
) : IRequest<Result>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class UpdateRestaurantLocationCommandValidator : AbstractValidator<UpdateRestaurantLocationCommand>
{
    public UpdateRestaurantLocationCommandValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ZipCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        When(x => x.Latitude.HasValue, () =>
        {
            RuleFor(x => x.Latitude!.Value).InclusiveBetween(-90, 90);
        });
        When(x => x.Longitude.HasValue, () =>
        {
            RuleFor(x => x.Longitude!.Value).InclusiveBetween(-180, 180);
        });
    }
}

public sealed class UpdateRestaurantLocationCommandHandler : IRequestHandler<UpdateRestaurantLocationCommand, Result>
{
    private readonly IRestaurantRepository _restaurants;
    private readonly IUnitOfWork _uow;

    public UpdateRestaurantLocationCommandHandler(IRestaurantRepository restaurants, IUnitOfWork uow)
    {
        _restaurants = restaurants;
        _uow = uow;
    }

    public async Task<Result> Handle(UpdateRestaurantLocationCommand request, CancellationToken cancellationToken)
    {
        return await _uow.ExecuteInTransactionAsync(async () =>
        {
            var id = RestaurantId.Create(request.RestaurantId);
            var restaurant = await _restaurants.GetByIdAsync(id, cancellationToken);
            if (restaurant is null)
            {
                return Result.Failure(RestaurantErrors.NotFound(request.RestaurantId));
            }

            var locRes = restaurant.ChangeLocation(request.Street, request.City, request.State, request.ZipCode, request.Country);
            if (locRes.IsFailure)
            {
                return locRes;
            }

            if (request.Latitude.HasValue && request.Longitude.HasValue)
            {
                var geoRes = restaurant.ChangeGeoCoordinates(request.Latitude.Value, request.Longitude.Value);
                if (geoRes.IsFailure) return geoRes;
            }

            await _restaurants.UpdateAsync(restaurant, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }, cancellationToken);
    }
}

public static class RestaurantErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Restaurant.NotFound",
        $"Restaurant '{id}' was not found.");
}
