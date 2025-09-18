using FluentValidation;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Restaurants.Commands.UpdateRestaurantProfile;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record UpdateRestaurantProfileCommand(
    Guid RestaurantId,
    string? Name,
    string? Description,
    string? LogoUrl,
    string? Phone,
    string? Email
) : IRequest<Result>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class UpdateRestaurantProfileCommandValidator : AbstractValidator<UpdateRestaurantProfileCommand>
{
    public UpdateRestaurantProfileCommandValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();

        RuleFor(x => x.Name).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);

        RuleFor(x => x)
            .Must(HaveAtLeastOneField)
            .WithMessage("At least one field (name, description, logoUrl, phone, email) must be provided.");
    }

    private static bool HaveAtLeastOneField(UpdateRestaurantProfileCommand c)
        => !(string.IsNullOrWhiteSpace(c.Name)
            && string.IsNullOrWhiteSpace(c.Description)
            && string.IsNullOrWhiteSpace(c.LogoUrl)
            && string.IsNullOrWhiteSpace(c.Phone)
            && string.IsNullOrWhiteSpace(c.Email));
}

public sealed class UpdateRestaurantProfileCommandHandler : IRequestHandler<UpdateRestaurantProfileCommand, Result>
{
    private readonly IRestaurantRepository _restaurants;
    private readonly IUnitOfWork _uow;

    public UpdateRestaurantProfileCommandHandler(IRestaurantRepository restaurants, IUnitOfWork uow)
    {
        _restaurants = restaurants;
        _uow = uow;
    }

    public async Task<Result> Handle(UpdateRestaurantProfileCommand request, CancellationToken cancellationToken)
    {
        return await _uow.ExecuteInTransactionAsync(async () =>
        {
            var id = RestaurantId.Create(request.RestaurantId);
            var restaurant = await _restaurants.GetByIdAsync(id, cancellationToken);
            if (restaurant is null)
            {
                return Result.Failure(RestaurantErrors.NotFound(request.RestaurantId));
            }

            if (request.Name is not null)
            {
                var r = restaurant.ChangeName(request.Name);
                if (r.IsFailure) return r;
            }

            if (request.Description is not null)
            {
                var r = restaurant.UpdateDescription(request.Description);
                if (r.IsFailure) return r;
            }

            if (request.LogoUrl is not null)
            {
                var r = restaurant.UpdateLogo(request.LogoUrl);
                if (r.IsFailure) return r;
            }

            if (request.Phone is not null || request.Email is not null)
            {
                var phone = request.Phone ?? restaurant.ContactInfo.PhoneNumber;
                var email = request.Email ?? restaurant.ContactInfo.Email;
                var r = restaurant.UpdateContactInfo(phone, email);
                if (r.IsFailure) return r;
            }

            await _restaurants.UpdateAsync(restaurant, cancellationToken);

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
