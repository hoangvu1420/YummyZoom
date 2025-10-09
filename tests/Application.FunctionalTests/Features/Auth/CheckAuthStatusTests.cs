using FluentAssertions;
using YummyZoom.Application.Auth.Commands.CheckAuthStatus;
using YummyZoom.Application.FunctionalTests.Common;

namespace YummyZoom.Application.FunctionalTests.Features.Auth;

using static Testing;

public class CheckAuthStatusTests : BaseTestFixture
{
    [Test]
    public async Task CheckAuthStatus_WithNewPhoneNumber_ReturnsNewUserStatus()
    {
        // Arrange
        var command = new CheckAuthStatusCommand("+15551234567", "US");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Success.Should().BeTrue();
        result.Value.Data.UserStatus.Should().Be(UserStatus.NewUser);
        result.Value.Data.UserExists.Should().BeFalse();
        result.Value.Data.HasPassword.Should().BeFalse();
        result.Value.Data.ProfileComplete.Should().BeFalse();
        result.Value.Data.UserInfo.Should().BeNull();
        result.Value.Data.SecurityInfo.PhoneVerified.Should().BeFalse();
        result.Value.Data.SecurityInfo.AccountLocked.Should().BeFalse();
    }

    [Test]
    public async Task CheckAuthStatus_WithInvalidPhoneFormat_ReturnsValidationError()
    {
        // Arrange
        var command = new CheckAuthStatusCommand("invalid-phone", "US");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("INVALID_PHONE_FORMAT");
    }

    [Test] 
    public async Task CheckAuthStatus_WithExistingUserNoPassword_ReturnsCorrectStatus()
    {
        // Arrange
        var phoneNumber = "+15550110004";
        
        // Create a user through OTP flow but don't set password
        var otpRequest = await SendAsync(new YummyZoom.Application.Auth.Commands.RequestPhoneOtp.RequestPhoneOtpCommand(phoneNumber));
        otpRequest.ShouldBeSuccessful();
        
        var verifyResult = await SendAsync(new YummyZoom.Application.Auth.Commands.VerifyPhoneOtp.VerifyPhoneOtpCommand(phoneNumber, "111111"));
        verifyResult.ShouldBeSuccessful();
        
        // Set user context for authenticated operations
        SetUserId(verifyResult.Value.IdentityUserId);
        
        var signupResult = await SendAsync(new YummyZoom.Application.Auth.Commands.CompleteSignup.CompleteSignupCommand("Test User", null));
        signupResult.ShouldBeSuccessful();

        var command = new CheckAuthStatusCommand(phoneNumber);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Data.UserStatus.Should().Be(UserStatus.ExistingNoPassword);
        result.Value.Data.UserExists.Should().BeTrue();
        result.Value.Data.HasPassword.Should().BeFalse();
        result.Value.Data.ProfileComplete.Should().BeTrue();
        result.Value.Data.UserInfo.Should().NotBeNull();
        result.Value.Data.UserInfo!.FirstName.Should().Be("Test User");
    }

    [Test]
    public async Task CheckAuthStatus_WithExistingUserWithPassword_ReturnsCorrectStatus()
    {
        // Arrange
        var phoneNumber = "+15550110005";
        
        // Create a user through OTP flow and set password
        var otpRequest = await SendAsync(new YummyZoom.Application.Auth.Commands.RequestPhoneOtp.RequestPhoneOtpCommand(phoneNumber));
        otpRequest.ShouldBeSuccessful();
        
        var verifyResult = await SendAsync(new YummyZoom.Application.Auth.Commands.VerifyPhoneOtp.VerifyPhoneOtpCommand(phoneNumber, "111111"));
        verifyResult.ShouldBeSuccessful();
        
        // Set user context for authenticated operations
        SetUserId(verifyResult.Value.IdentityUserId);
        
        var signupResult = await SendAsync(new YummyZoom.Application.Auth.Commands.CompleteSignup.CompleteSignupCommand("Test User With Password", null));
        signupResult.ShouldBeSuccessful();
        
        // Set password for the user
        var passwordResult = await SendAsync(new YummyZoom.Application.Auth.Commands.SetPassword.SetPasswordCommand("TestPassword123!"));
        passwordResult.ShouldBeSuccessful();

        var command = new CheckAuthStatusCommand(phoneNumber);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Data.UserStatus.Should().Be(UserStatus.ExistingWithPassword);
        result.Value.Data.UserExists.Should().BeTrue();
        result.Value.Data.HasPassword.Should().BeTrue();
        result.Value.Data.ProfileComplete.Should().BeTrue();
        result.Value.Data.UserInfo.Should().NotBeNull();
        result.Value.Data.UserInfo!.FirstName.Should().Be("Test User With Password");
    }
}
