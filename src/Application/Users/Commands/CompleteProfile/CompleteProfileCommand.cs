using FluentValidation;
using MediatR;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Users.Commands.CompleteProfile;

/// <summary>
/// Updates profile details for an existing domain user.
/// Use <see cref="YummyZoom.Application.Auth.Commands.CompleteSignup.CompleteSignupCommand"/>
/// to create the initial domain user after OTP verification.
/// </summary>
public record CompleteProfileCommand(string Name, string? Email) : IRequest<Result>;

public class CompleteProfileCommandValidator : AbstractValidator<CompleteProfileCommand>
{
    public CompleteProfileCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class CompleteProfileCommandHandler : IRequestHandler<CompleteProfileCommand, Result>
{
    private static readonly Error Unauthorized = Error.Problem("Auth.Unauthorized", "User is not authenticated.");

    private readonly IUser _currentUser;
    private readonly IUserAggregateRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteProfileCommandHandler(
        IUser currentUser,
        IUserAggregateRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result> Handle(CompleteProfileCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.DomainUserId is null)
        {
            return Result.Failure(Unauthorized);
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var userId = _currentUser.DomainUserId;
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                return Result.Failure(UserErrors.UserNotFound(userId.Value));
            }

            var profileResult = user.UpdateProfile(request.Name.Trim(), user.PhoneNumber);
            if (profileResult.IsFailure)
            {
                return profileResult;
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var emailResult = user.UpdateEmail(request.Email.Trim());
                if (emailResult.IsFailure)
                {
                    return emailResult;
                }
            }

            await _userRepository.UpdateAsync(user, cancellationToken);
            return Result.Success();
        }, cancellationToken);
    }
}
