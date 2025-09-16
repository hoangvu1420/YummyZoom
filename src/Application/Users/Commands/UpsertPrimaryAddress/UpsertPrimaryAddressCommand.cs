using FluentValidation;
using MediatR;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.UserAggregate.Entities;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Users.Commands.UpsertPrimaryAddress;

public record UpsertPrimaryAddressCommand(
    string Street,
    string City,
    string? State,
    string ZipCode,
    string Country,
    string? Label,
    string? DeliveryInstructions) : IRequest<Result<Guid>>;

public class UpsertPrimaryAddressCommandValidator : AbstractValidator<UpsertPrimaryAddressCommand>
{
    public UpsertPrimaryAddressCommandValidator()
    {
        RuleFor(x => x.Street).NotEmpty().MaximumLength(255);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(100);
        RuleFor(x => x.ZipCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Label).MaximumLength(100);
        RuleFor(x => x.DeliveryInstructions).MaximumLength(500);
    }
}

public class UpsertPrimaryAddressCommandHandler : IRequestHandler<UpsertPrimaryAddressCommand, Result<Guid>>
{
    private static readonly Error Unauthorized = Error.Problem("Auth.Unauthorized", "User is not authenticated.");

    private readonly IUser _currentUser;
    private readonly IUserAggregateRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpsertPrimaryAddressCommandHandler(
        IUser currentUser,
        IUserAggregateRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Guid>> Handle(UpsertPrimaryAddressCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.DomainUserId is null)
        {
            return Result.Failure<Guid>(Unauthorized);
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var userId = _currentUser.DomainUserId;
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                return Result.Failure<Guid>(UserErrors.UserNotFound(userId.Value));
            }

            var primary = user.Addresses.FirstOrDefault();

            if (primary is null)
            {
                var created = Address.Create(
                    request.Street.Trim(),
                    request.City.Trim(),
                    request.State?.Trim() ?? string.Empty,
                    request.ZipCode.Trim(),
                    request.Country.Trim(),
                    request.Label?.Trim(),
                    request.DeliveryInstructions?.Trim());

                var addResult = user.AddAddress(created);
                if (addResult.IsFailure)
                {
                    return Result.Failure<Guid>(addResult.Error);
                }

                await _userRepository.UpdateAsync(user, cancellationToken);
                return Result.Success(created.Id.Value);
            }

            // Update existing primary address
            primary.UpdateDetails(
                request.Street.Trim(),
                request.City.Trim(),
                request.State?.Trim() ?? string.Empty,
                request.ZipCode.Trim(),
                request.Country.Trim(),
                request.Label?.Trim(),
                request.DeliveryInstructions?.Trim());

            // Remove any extra addresses to keep a single primary entry in MVP scope
            foreach (var extra in user.Addresses.Skip(1).ToList())
            {
                user.RemoveAddress(extra.Id);
            }

            await _userRepository.UpdateAsync(user, cancellationToken);
            return Result.Success(primary.Id.Value);
        }, cancellationToken);
    }
}
