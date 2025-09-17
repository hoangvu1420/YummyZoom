using FluentValidation;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.RestaurantRegistrationAggregate;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.RestaurantRegistrations.Commands.SubmitRestaurantRegistration;

[Authorize(Policy = Policies.CompletedSignup)]
public sealed record SubmitRestaurantRegistrationCommand(
    string Name,
    string Description,
    string CuisineType,
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country,
    string PhoneNumber,
    string Email,
    string BusinessHours,
    string? LogoUrl,
    double? Latitude,
    double? Longitude
) : IRequest<Result<SubmitRestaurantRegistrationResponse>>, IUserCommand
{
    // IUserCommand implementation (resource binding to current user)
    public required Domain.UserAggregate.ValueObjects.UserId UserId { get; init; }
}

public sealed record SubmitRestaurantRegistrationResponse(Guid RegistrationId);

public sealed class SubmitRestaurantRegistrationCommandValidator : AbstractValidator<SubmitRestaurantRegistrationCommand>
{
    public SubmitRestaurantRegistrationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CuisineType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ZipCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.BusinessHours).NotEmpty().MaximumLength(200);
        When(x => !string.IsNullOrWhiteSpace(x.LogoUrl), () =>
        {
            RuleFor(x => x.LogoUrl!).Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("LogoUrl must be a valid absolute URL");
        });
        When(x => x.Latitude.HasValue, () => RuleFor(x => x.Latitude!.Value).InclusiveBetween(-90, 90));
        When(x => x.Longitude.HasValue, () => RuleFor(x => x.Longitude!.Value).InclusiveBetween(-180, 180));
    }
}

public sealed class SubmitRestaurantRegistrationCommandHandler : IRequestHandler<SubmitRestaurantRegistrationCommand, Result<SubmitRestaurantRegistrationResponse>>
{
    private readonly IRestaurantRegistrationRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IUser _user;

    public SubmitRestaurantRegistrationCommandHandler(
        IRestaurantRegistrationRepository repository,
        IUnitOfWork uow,
        IUser user)
    {
        _repository = repository;
        _uow = uow;
        _user = user;
    }

    public Task<Result<SubmitRestaurantRegistrationResponse>> Handle(SubmitRestaurantRegistrationCommand request, CancellationToken cancellationToken)
    {
        return _uow.ExecuteInTransactionAsync(async () =>
        {
            var domainUserId = _user.DomainUserId;
            if (domainUserId is null)
            {
                return Result.Failure<SubmitRestaurantRegistrationResponse>(UserErrors.UserNotFound(Guid.Empty));
            }

            var regResult = RestaurantRegistration.Submit(
                domainUserId,
                request.Name,
                request.Description,
                request.CuisineType,
                request.Street,
                request.City,
                request.State,
                request.ZipCode,
                request.Country,
                request.PhoneNumber,
                request.Email,
                request.BusinessHours,
                request.LogoUrl,
                request.Latitude,
                request.Longitude);

            if (regResult.IsFailure)
            {
                return Result.Failure<SubmitRestaurantRegistrationResponse>(regResult.Error);
            }

            await _repository.AddAsync(regResult.Value, cancellationToken);

            return Result.Success(new SubmitRestaurantRegistrationResponse(regResult.Value.Id.Value));
        }, cancellationToken);
    }
}

