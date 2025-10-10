using FluentValidation;
using MediatR;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Auth.Commands.CompleteSignup;

public record CompleteSignupCommand(
    string Name,
    string? Email = null
) : IRequest<Result>;

public class CompleteSignupCommandValidator : AbstractValidator<CompleteSignupCommand>
{
    public CompleteSignupCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class CompleteSignupCommandHandler : IRequestHandler<CompleteSignupCommand, Result>
{
    private static readonly Error Unauthorized = Error.Problem("Auth.Unauthorized", "User is not authenticated.");

    private readonly IUser _currentUser;
    private readonly IUserAggregateRepository _userRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteSignupCommandHandler(
        IUser currentUser,
        IUserAggregateRepository userRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result> Handle(CompleteSignupCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is null)
        {
            return Result.Failure(Unauthorized);
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Identity user id is the domain user id (one-to-one)
            var userGuid = Guid.Parse(_currentUser.Id);
            var domainUserId = UserId.Create(userGuid);

            // Check if user already exists
            var existing = await _userRepository.GetByIdAsync(domainUserId, cancellationToken);
            
            var name = request.Name.Trim();
            var email = string.IsNullOrWhiteSpace(request.Email) ? $"{userGuid:N}@signup.temp" : request.Email.Trim();

            if (existing is not null)
            {
                // Update existing user with new values (idempotent but updates profile)
                // Preserve existing phone number instead of getting from Identity username (which might have been changed to email)
                var updateProfileResult = existing.UpdateProfile(name, existing.PhoneNumber);
                if (updateProfileResult.IsFailure)
                {
                    return Result.Failure(updateProfileResult.Error);
                }

                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    var updateEmailResult = existing.UpdateEmail(email);
                    if (updateEmailResult.IsFailure)
                    {
                        return Result.Failure(updateEmailResult.Error);
                    }
                }

                // Update domain user
                await _userRepository.UpdateAsync(existing, cancellationToken);

                // Also update Identity user to keep them in sync
                // Update email in Identity if provided and different from temp email
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    var identityUpdateResult = await _identityService.UpdateEmailAsync(_currentUser.Id!, email);
                    if (identityUpdateResult.IsFailure)
                    {
                        return identityUpdateResult;
                    }
                }

                return Result.Success();
            }

            // Create new user if doesn't exist
            // Get phone from Identity username (OTP flow sets username to phone E.164)
            var username = await _identityService.GetUserNameAsync(_currentUser.Id);
            var phoneE164 = username; // may be null or non-phone in other auth modes

            var createResult = User.Create(
                domainUserId,
                name,
                email,
                phoneE164,
                isActive: true);

            if (createResult.IsFailure)
            {
                return Result.Failure(createResult.Error);
            }

            await _userRepository.AddAsync(createResult.Value, cancellationToken);

            // Also update Identity user to keep them in sync when creating new domain user
            // Update email in Identity if provided and different from temp email
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var identityUpdateResult = await _identityService.UpdateEmailAsync(_currentUser.Id!, email);
                if (identityUpdateResult.IsFailure)
                {
                    return identityUpdateResult;
                }
            }

            return Result.Success();
        }, cancellationToken);
    }
}

