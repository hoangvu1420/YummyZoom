using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using YummyZoom.Application.Auth.Commands.RequestPhoneOtp;
using YummyZoom.Application.Auth.Commands.VerifyPhoneOtp;
using YummyZoom.Application.Common.Configuration;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;

namespace YummyZoom.Application.FunctionalTests.Features.Auth;

using static Testing;

public class OtpThrottlingTests : BaseTestFixture
{
    private IOtpThrottleStore _throttleStore = null!;
    private RateLimitingOptions _rateLimitingOptions = null!;

    [SetUp]
    public void SetUp()
    {
        _throttleStore = GetService<IOtpThrottleStore>();
        _rateLimitingOptions = GetService<IOptions<RateLimitingOptions>>().Value;
    }

    [Test]
    public async Task RequestPhoneOtp_WhenWithinLimits_ShouldSucceed()
    {
        // Arrange
        var phoneNumber = "+15551234567";
        var command = new RequestPhoneOtpCommand(phoneNumber);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task RequestPhoneOtp_WhenExceedsPerMinuteLimit_ShouldReturnThrottled()
    {
        // Arrange
        var phoneNumber = "+15551234568";
        var perMinuteLimit = _rateLimitingOptions.OtpRequest.PerPhone.PerMinute;

        // Simulate exceeding the per-minute limit
        for (int i = 0; i < perMinuteLimit; i++)
        {
            await _throttleStore.IncrementRequestCountAsync(phoneNumber, 1);
        }

        var command = new RequestPhoneOtpCommand(phoneNumber);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Otp.Throttled");
        result.Error.Description.Should().Contain("seconds");
    }

    [Test]
    public async Task RequestPhoneOtp_OneRequestPerMinute_ShouldEnforceStrictLimit()
    {
        // Arrange
        var phoneNumber = "+15551234575";
        
        // Verify the limit is set to 1 per minute
        _rateLimitingOptions.OtpRequest.PerPhone.PerMinute.Should().Be(1);

        // Act & Assert - First request should succeed
        var firstRequest = new RequestPhoneOtpCommand(phoneNumber);
        var firstResult = await SendAsync(firstRequest);
        firstResult.ShouldBeSuccessful();

        // Second request within the same minute should be throttled
        var secondRequest = new RequestPhoneOtpCommand(phoneNumber);
        var secondResult = await SendAsync(secondRequest);
        
        // Assert
        secondResult.IsFailure.Should().BeTrue();
        secondResult.Error.Code.Should().Be("Otp.Throttled");
        secondResult.Error.Description.Should().Contain("Too many requests");
        secondResult.Error.Description.Should().Contain("seconds");
    }

    [Test]
    public async Task RequestPhoneOtp_WhenExceedsPerHourLimit_ShouldReturnThrottled()
    {
        // Arrange
        var phoneNumber = "+15551234569";
        var perHourLimit = _rateLimitingOptions.OtpRequest.PerPhone.PerHour;

        // Simulate exceeding the per-hour limit
        for (int i = 0; i < perHourLimit; i++)
        {
            await _throttleStore.IncrementRequestCountAsync(phoneNumber, 60);
        }

        var command = new RequestPhoneOtpCommand(phoneNumber);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Otp.Throttled");
        result.Error.Description.Should().Contain("seconds");
    }

    [Test]
    public async Task VerifyPhoneOtp_WhenLockedOut_ShouldReturnLockedOut()
    {
        // Arrange
        var phoneNumber = "+15551234570";
        var lockoutMinutes = _rateLimitingOptions.OtpVerify.PerPhone.LockoutMinutes;

        // Set lockout
        await _throttleStore.SetLockoutAsync(phoneNumber, lockoutMinutes);

        var command = new VerifyPhoneOtpCommand(phoneNumber, "123456");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Otp.LockedOut");
        result.Error.Description.Should().Contain("locked");
    }

    [Test]
    public async Task VerifyPhoneOtp_WhenExceedsFailedAttempts_ShouldSetLockout()
    {
        // Arrange
        var phoneNumber = "+15551234571";
        var maxFailedAttempts = _rateLimitingOptions.OtpVerify.PerPhone.MaxFailedAttempts;

        // First request an OTP to ensure the phone number exists in the system
        var otpRequest = await SendAsync(new RequestPhoneOtpCommand(phoneNumber));
        otpRequest.ShouldBeSuccessful();

        // Simulate failed attempts up to the limit
        for (int i = 0; i < maxFailedAttempts - 1; i++)
        {
            await _throttleStore.RecordFailedVerifyAsync(phoneNumber);
        }

        // This should trigger lockout (using wrong numeric code to ensure failure)
        var command = new VerifyPhoneOtpCommand(phoneNumber, "999999");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        
        // Verify lockout was set
        var isLockedOut = await _throttleStore.IsLockedOutAsync(phoneNumber);
        isLockedOut.Should().BeTrue();
    }

    [Test]
    public async Task OtpThrottleStore_ShouldHandleRequestCounting()
    {
        // Arrange
        var phoneNumber = "+15551234572";

        // Act & Assert
        var initialCount = await _throttleStore.GetRequestCountAsync(phoneNumber, 1);
        initialCount.Should().Be(0);

        var newCount = await _throttleStore.IncrementRequestCountAsync(phoneNumber, 1);
        newCount.Should().Be(1);

        var currentCount = await _throttleStore.GetRequestCountAsync(phoneNumber, 1);
        currentCount.Should().Be(1);

        // Reset and verify
        await _throttleStore.ResetRequestCountAsync(phoneNumber);
        var resetCount = await _throttleStore.GetRequestCountAsync(phoneNumber, 1);
        resetCount.Should().Be(0);
    }

    [Test]
    public async Task OtpThrottleStore_ShouldHandleFailedVerificationTracking()
    {
        // Arrange
        var phoneNumber = "+15551234573";

        // Act & Assert
        var initialCount = await _throttleStore.GetFailedVerifyCountAsync(phoneNumber, 10);
        initialCount.Should().Be(0);

        var newCount = await _throttleStore.RecordFailedVerifyAsync(phoneNumber);
        newCount.Should().Be(1);

        var currentCount = await _throttleStore.GetFailedVerifyCountAsync(phoneNumber, 10);
        currentCount.Should().Be(1);

        // Reset and verify
        await _throttleStore.ResetFailedVerifyAndLockoutAsync(phoneNumber);
        var resetCount = await _throttleStore.GetFailedVerifyCountAsync(phoneNumber, 10);
        resetCount.Should().Be(0);
    }

    [Test]
    public async Task OtpThrottleStore_ShouldHandleLockoutManagement()
    {
        // Arrange
        var phoneNumber = "+15551234574";
        var lockoutMinutes = 2;

        // Act & Assert
        var initialLockout = await _throttleStore.IsLockedOutAsync(phoneNumber);
        initialLockout.Should().BeFalse();

        await _throttleStore.SetLockoutAsync(phoneNumber, lockoutMinutes);

        var isLockedOut = await _throttleStore.IsLockedOutAsync(phoneNumber);
        isLockedOut.Should().BeTrue();

        var remainingSeconds = await _throttleStore.GetLockoutRemainingSecondsAsync(phoneNumber);
        remainingSeconds.Should().BeGreaterThan(0);
        remainingSeconds.Should().BeLessOrEqualTo(lockoutMinutes * 60);

        // Reset and verify
        await _throttleStore.ResetFailedVerifyAndLockoutAsync(phoneNumber);
        var resetLockout = await _throttleStore.IsLockedOutAsync(phoneNumber);
        resetLockout.Should().BeFalse();
    }
}
