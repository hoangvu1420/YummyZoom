using FluentValidation;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Restaurants.Commands.UpdateRestaurantBusinessHours;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record UpdateRestaurantBusinessHoursCommand(
    Guid RestaurantId,
    string BusinessHours
) : IRequest<Result>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class UpdateRestaurantBusinessHoursCommandValidator : AbstractValidator<UpdateRestaurantBusinessHoursCommand>
{
    public UpdateRestaurantBusinessHoursCommandValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.BusinessHours)
            .NotEmpty()
            .MaximumLength(200)
            .Must(BeValidHourFormat)
            .WithMessage("Business hours must be in format hh:mm-hh:mm (e.g., '09:00-17:30')");
    }

    private static bool BeValidHourFormat(string hours)
    {
        if (string.IsNullOrWhiteSpace(hours))
            return false;

        // Use the same validation logic as the domain
        var result = BusinessHours.Create(hours.Trim());
        return result.IsSuccess;
    }
}

public sealed class UpdateRestaurantBusinessHoursCommandHandler : IRequestHandler<UpdateRestaurantBusinessHoursCommand, Result>
{
    private readonly IRestaurantRepository _restaurants;
    private readonly IUnitOfWork _uow;

    public UpdateRestaurantBusinessHoursCommandHandler(IRestaurantRepository restaurants, IUnitOfWork uow)
    {
        _restaurants = restaurants;
        _uow = uow;
    }

    public async Task<Result> Handle(UpdateRestaurantBusinessHoursCommand request, CancellationToken cancellationToken)
    {
        return await _uow.ExecuteInTransactionAsync(async () =>
        {
            var id = RestaurantId.Create(request.RestaurantId);
            var restaurant = await _restaurants.GetByIdAsync(id, cancellationToken);
            if (restaurant is null)
            {
                return Result.Failure(RestaurantErrors.NotFound(request.RestaurantId));
            }

            var updated = restaurant.UpdateBusinessHours(request.BusinessHours);
            if (updated.IsFailure)
            {
                return updated;
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

