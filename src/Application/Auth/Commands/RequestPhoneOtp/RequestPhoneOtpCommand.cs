using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using YummyZoom.Application.Common.Configuration;
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
    private readonly IOtpThrottleStore _throttleStore;
    private readonly RateLimitingOptions _rateLimitingOptions;

    public RequestPhoneOtpCommandHandler(
        IPhoneNumberNormalizer normalizer,
        IPhoneOtpService otpService,
        ISmsSender smsSender,
        IOtpThrottleStore throttleStore,
        IOptions<RateLimitingOptions> rateLimitingOptions)
    {
        _normalizer = normalizer;
        _otpService = otpService;
        _smsSender = smsSender;
        _throttleStore = throttleStore;
        _rateLimitingOptions = rateLimitingOptions.Value;
    }

    public async Task<Result<RequestPhoneOtpResponse>> Handle(RequestPhoneOtpCommand request, CancellationToken cancellationToken)
    {
        var phone = _normalizer.Normalize(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(phone))
            return Result.Failure<RequestPhoneOtpResponse>(Error.Validation("Phone.Invalid", "Phone number is invalid."));

        // Check per-phone throttling limits
        var perMinuteLimit = _rateLimitingOptions.OtpRequest.PerPhone.PerMinute;
        var perHourLimit = _rateLimitingOptions.OtpRequest.PerPhone.PerHour;

        // Check 1-minute window
        var currentMinuteCount = await _throttleStore.GetRequestCountAsync(phone!, 1, cancellationToken);
        if (currentMinuteCount >= perMinuteLimit)
        {
            var retryAfterSeconds = await _throttleStore.GetRetryAfterSecondsAsync(phone!, 1, cancellationToken);
            return Result.Failure<RequestPhoneOtpResponse>(
                Error.Problem("Otp.Throttled", $"Too many requests. Please try again in {retryAfterSeconds} seconds."));
        }

        // Check 1-hour window
        var currentHourCount = await _throttleStore.GetRequestCountAsync(phone!, 60, cancellationToken);
        if (currentHourCount >= perHourLimit)
        {
            var retryAfterSeconds = await _throttleStore.GetRetryAfterSecondsAsync(phone!, 60, cancellationToken);
            return Result.Failure<RequestPhoneOtpResponse>(
                Error.Problem("Otp.Throttled", $"Too many requests. Please try again in {retryAfterSeconds} seconds."));
        }

        // Increment request count before processing
        await _throttleStore.IncrementRequestCountAsync(phone!, 1, cancellationToken);
        await _throttleStore.IncrementRequestCountAsync(phone!, 60, cancellationToken);

        var ensure = await _otpService.EnsureUserExistsAsync(phone!, cancellationToken);
        if (ensure.IsFailure) return Result.Failure<RequestPhoneOtpResponse>(ensure.Error);

        var codeResult = await _otpService.GenerateLoginCodeAsync(ensure.Value.IdentityUserId, cancellationToken);
        if (codeResult.IsFailure) return Result.Failure<RequestPhoneOtpResponse>(codeResult.Error);

        await _smsSender.SendAsync(phone!, $"Your YummyZoom code is {codeResult.Value}", cancellationToken);
        return Result.Success(new RequestPhoneOtpResponse(codeResult.Value));
    }
}

