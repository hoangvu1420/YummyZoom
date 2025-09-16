using FluentValidation;
using MediatR;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Auth.Commands.RequestPhoneOtp;

public record RequestPhoneOtpCommand(string PhoneNumber) : IRequest<Result<RequestPhoneOtpResponse>>;

public record RequestPhoneOtpResponse(string Code);

public class RequestPhoneOtpCommandValidator : AbstractValidator<RequestPhoneOtpCommand>
{
    public RequestPhoneOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .MaximumLength(50);
    }
}

public class RequestPhoneOtpCommandHandler : IRequestHandler<RequestPhoneOtpCommand, Result<RequestPhoneOtpResponse>>
{
    private readonly IPhoneNumberNormalizer _normalizer;
    private readonly IPhoneOtpService _otpService;
    private readonly ISmsSender _smsSender;

    public RequestPhoneOtpCommandHandler(
        IPhoneNumberNormalizer normalizer,
        IPhoneOtpService otpService,
        ISmsSender smsSender)
    {
        _normalizer = normalizer;
        _otpService = otpService;
        _smsSender = smsSender;
    }

    public async Task<Result<RequestPhoneOtpResponse>> Handle(RequestPhoneOtpCommand request, CancellationToken cancellationToken)
    {
        var phone = _normalizer.Normalize(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(phone))
            return Result.Failure<RequestPhoneOtpResponse>(Error.Validation("Phone.Invalid", "Phone number is invalid."));

        var ensure = await _otpService.EnsureUserExistsAsync(phone!, cancellationToken);
        if (ensure.IsFailure) return Result.Failure<RequestPhoneOtpResponse>(ensure.Error);

        var codeResult = await _otpService.GenerateLoginCodeAsync(ensure.Value.IdentityUserId, cancellationToken);
        if (codeResult.IsFailure) return Result.Failure<RequestPhoneOtpResponse>(codeResult.Error);

        await _smsSender.SendAsync(phone!, $"Your YummyZoom code is {codeResult.Value}", cancellationToken);
        return Result.Success(new RequestPhoneOtpResponse(codeResult.Value));
    }
}

