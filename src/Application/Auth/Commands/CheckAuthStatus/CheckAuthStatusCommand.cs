using FluentValidation;
using MediatR;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Auth.Commands.CheckAuthStatus;

public record CheckAuthStatusCommand(string Phone, string? CountryCode = null) : IRequest<Result<CheckAuthStatusResponse>>;

public class CheckAuthStatusCommandValidator : AbstractValidator<CheckAuthStatusCommand>
{
    public CheckAuthStatusCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithMessage("Phone number is required.")
            .MaximumLength(20)
            .WithMessage("Phone number is too long.");

        RuleFor(x => x.CountryCode)
            .MaximumLength(5)
            .WithMessage("Country code is too long.")
            .When(x => !string.IsNullOrEmpty(x.CountryCode));
    }
}

public class CheckAuthStatusCommandHandler : IRequestHandler<CheckAuthStatusCommand, Result<CheckAuthStatusResponse>>
{
    private readonly IPhoneNumberNormalizer _phoneNormalizer;
    private readonly IIdentityService _identityService;
    private readonly IUserAggregateRepository _userRepository;
    private readonly IOtpThrottleStore _throttleStore;
    private readonly IUserDeviceSessionRepository _sessionRepository;

    public CheckAuthStatusCommandHandler(
        IPhoneNumberNormalizer phoneNormalizer,
        IIdentityService identityService,
        IUserAggregateRepository userRepository,
        IOtpThrottleStore throttleStore,
        IUserDeviceSessionRepository sessionRepository)
    {
        _phoneNormalizer = phoneNormalizer;
        _identityService = identityService;
        _userRepository = userRepository;
        _throttleStore = throttleStore;
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<CheckAuthStatusResponse>> Handle(CheckAuthStatusCommand request, CancellationToken cancellationToken)
    {
        // Normalize phone number to E.164 format
        var normalizedPhone = _phoneNormalizer.Normalize(request.Phone);
        if (string.IsNullOrEmpty(normalizedPhone))
        {
            return Result.Failure<CheckAuthStatusResponse>(
                Error.Problem("INVALID_PHONE_FORMAT", "Phone number must be in E.164 format"));
        }

        try
        {
            // Find Identity user by phone number (stored in UserName field)
            var identityUserId = await _identityService.FindUserIdByPhoneAsync(normalizedPhone);

            if (identityUserId == null)
            {
                // New user - phone number not found in system
                return Result.Success(new CheckAuthStatusResponse
                {
                    Success = true,
                    Data = new CheckAuthStatusData
                    {
                        UserStatus = UserStatus.NewUser,
                        UserExists = false,
                        HasPassword = false,
                        ProfileComplete = false,
                        UserInfo = null,
                        SecurityInfo = new SecurityInfo
                        {
                            PhoneVerified = false,
                            AccountLocked = false,
                            FailedAttempts = 0,
                            LockoutUntil = null
                        }
                    }
                });
            }

            // Check if user has password set
            var hasPassword = await _identityService.HasPasswordAsync(identityUserId.Value);

            // Check if account is locked
            var isLockedOut = await _identityService.IsLockedOutAsync(identityUserId.Value);
            var lockoutEnd = await _identityService.GetLockoutEndDateAsync(identityUserId.Value);

            // Get failed attempt count from throttle store
            var failedAttempts = await _throttleStore.GetFailedVerifyCountAsync(normalizedPhone, 10, cancellationToken);

            // Check if domain user exists (profile complete)
            var domainUserId = UserId.Create(identityUserId.Value);
            var domainUser = await _userRepository.GetByIdAsync(domainUserId, cancellationToken);
            var profileComplete = domainUser is not null;

            // Determine user status
            var userStatus = isLockedOut ? UserStatus.AccountLocked :
                            hasPassword ? UserStatus.ExistingWithPassword :
                            UserStatus.ExistingNoPassword;

            // Prepare user info if profile is complete
            UserInfo? userInfo = null;
            if (profileComplete && domainUser is not null)
            {
                // Get last login from the most recent device session
                var lastLogin = await _sessionRepository.GetLastLoginAsync(identityUserId.Value, cancellationToken);

                userInfo = new UserInfo
                {
                    FirstName = domainUser.Name,
                    UserId = domainUser.Id.Value.ToString(),
                    LastLogin = lastLogin,
                    AccountCreated = domainUser.Created
                };
            }

            // Check if phone is verified
            var phoneVerified = await _identityService.IsPhoneNumberConfirmedAsync(identityUserId.Value);

            var response = new CheckAuthStatusResponse
            {
                Success = true,
                Data = new CheckAuthStatusData
                {
                    UserStatus = userStatus,
                    UserExists = true,
                    HasPassword = hasPassword,
                    ProfileComplete = profileComplete,
                    UserInfo = userInfo,
                    SecurityInfo = new SecurityInfo
                    {
                        PhoneVerified = phoneVerified,
                        AccountLocked = isLockedOut,
                        FailedAttempts = failedAttempts,
                        LockoutUntil = lockoutEnd?.DateTime
                    }
                }
            };

            return Result.Success(response);
        }
        catch (Exception)
        {
            return Result.Failure<CheckAuthStatusResponse>(
                Error.Problem("SERVICE_UNAVAILABLE", "Database or service error occurred"));
        }
    }
}
