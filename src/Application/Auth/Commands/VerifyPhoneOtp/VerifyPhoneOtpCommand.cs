using FluentValidation;
using MediatR;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Auth.Commands.VerifyPhoneOtp;

public record VerifyPhoneOtpCommand(string PhoneNumber, string Code) : IRequest<Result<VerifyPhoneOtpResponse>>;

public record VerifyPhoneOtpResponse(
    Guid IdentityUserId,
    bool IsNewUser,
    bool RequiresOnboarding);

public class VerifyPhoneOtpCommandValidator : AbstractValidator<VerifyPhoneOtpCommand>
{
    public VerifyPhoneOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(4, 8)
            .Matches("^[0-9]+$").WithMessage("Code must be numeric.");
    }
}

public class VerifyPhoneOtpCommandHandler : IRequestHandler<VerifyPhoneOtpCommand, Result<VerifyPhoneOtpResponse>>
{
    private readonly IPhoneNumberNormalizer _normalizer;
    private readonly IPhoneOtpService _otpService;
    private readonly IUserAggregateRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyPhoneOtpCommandHandler(
        IPhoneNumberNormalizer normalizer,
        IPhoneOtpService otpService,
        IUserAggregateRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _normalizer = normalizer;
        _otpService = otpService;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<VerifyPhoneOtpResponse>> Handle(VerifyPhoneOtpCommand request, CancellationToken cancellationToken)
    {
        var phone = _normalizer.Normalize(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(phone))
            return Result.Failure<VerifyPhoneOtpResponse>(Error.Validation("Phone.Invalid", "Phone number is invalid."));

        var userIdResult = await _otpService.FindByPhoneAsync(phone!, cancellationToken);
        if (userIdResult.IsFailure)
            return Result.Failure<VerifyPhoneOtpResponse>(userIdResult.Error);

        if (userIdResult.Value is null)
            return Result.Failure<VerifyPhoneOtpResponse>(Error.Problem("Otp.Invalid", "Invalid or expired code."));

        var identityUserId = userIdResult.Value.Value;

        var verify = await _otpService.VerifyLoginCodeAsync(identityUserId, request.Code, cancellationToken);
        if (verify.IsFailure)
            return Result.Failure<VerifyPhoneOtpResponse>(verify.Error);

        if (!verify.Value)
            return Result.Failure<VerifyPhoneOtpResponse>(Error.Problem("Otp.Invalid", "Invalid or expired code."));

        await _otpService.ConfirmPhoneAsync(identityUserId, cancellationToken);

        // Determine if a domain user already exists for this identity
        var domainUserId = UserId.Create(identityUserId);
        var existingDomainUser = await _userRepository.GetByIdAsync(domainUserId, cancellationToken);

        var isNewUser = existingDomainUser is null;
        var requiresOnboarding = isNewUser; // First-time signup requires onboarding

        return Result.Success(new VerifyPhoneOtpResponse(
            identityUserId,
            IsNewUser: isNewUser,
            RequiresOnboarding: requiresOnboarding));
    }
}

