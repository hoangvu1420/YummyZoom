using FluentValidation;
using MediatR;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Auth.Commands.VerifyPhoneOtp;

public record VerifyPhoneOtpCommand(string PhoneNumber, string Code) : IRequest<Result<VerifyPhoneOtpResponse>>;

public record VerifyPhoneOtpResponse(Guid IdentityUserId);

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

    public VerifyPhoneOtpCommandHandler(
        IPhoneNumberNormalizer normalizer,
        IPhoneOtpService otpService)
    {
        _normalizer = normalizer;
        _otpService = otpService;
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
            return Result.Failure<VerifyPhoneOtpResponse>(Error.Unauthorized("Otp.Invalid", "Invalid or expired code."));

        var verify = await _otpService.VerifyLoginCodeAsync(userIdResult.Value.Value, request.Code, cancellationToken);
        if (verify.IsFailure)
            return Result.Failure<VerifyPhoneOtpResponse>(verify.Error);

        if (!verify.Value)
            return Result.Failure<VerifyPhoneOtpResponse>(Error.Unauthorized("Otp.Invalid", "Invalid or expired code."));

        await _otpService.ConfirmPhoneAsync(userIdResult.Value.Value, cancellationToken);

        return Result.Success(new VerifyPhoneOtpResponse(userIdResult.Value.Value));
    }
}

