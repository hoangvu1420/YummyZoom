using YummyZoom.Application.Auth.Commands.RequestPhoneOtp;
using YummyZoom.Application.Auth.Commands.VerifyPhoneOtp;
using YummyZoom.Application.Auth.Commands.CompleteSignup;
using YummyZoom.Application.Users.Commands.CompleteProfile;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.FunctionalTests.Features.Auth;

using static Testing;

public class PhoneOtpValidationAndAuthTests : BaseTestFixture
{
    private const string PhoneNumber = "+15550110003";

    [SetUp]
    public async Task EnsureUserRoleAsync()
    {
        await EnsureRolesExistAsync(Roles.User);
    }

    [Test]
    public async Task VerifyPhoneOtp_WithInvalidCode_ShouldFail()
    {
        // Arrange
        var otp = await SendAsync(new RequestPhoneOtpCommand(PhoneNumber));
        otp.ShouldBeSuccessful();
        var wrong = "000000"; // guaranteed wrong vs default 111111

        // Act
        var verify = await SendAsync(new VerifyPhoneOtpCommand(PhoneNumber, wrong));

        // Assert
        verify.ShouldBeFailure("Otp.Invalid");
    }

    [Test]
    public async Task CompleteSignup_WithoutAuthentication_ShouldBeUnauthorized()
    {
        // Arrange: ensure no user in context
        SetUserId(null);

        // Act
        var result = await SendAsync(new CompleteSignupCommand("Anonymous", null));

        // Assert
        result.ShouldBeFailure("Auth.Unauthorized");
    }

    [Test]
    public async Task CompleteProfile_BeforeSignup_ShouldFailWithUserNotFound()
    {
        // Arrange: verify OTP to authenticate, but do NOT complete signup
        var otp = await SendAsync(new RequestPhoneOtpCommand(PhoneNumber));
        otp.ShouldBeSuccessful();
        var code = otp.Value.Code;

        var verify = await SendAsync(new VerifyPhoneOtpCommand(PhoneNumber, code));
        verify.ShouldBeSuccessful();
        SetUserId(verify.Value.IdentityUserId);

        // Act
        var profile = await SendAsync(new CompleteProfileCommand("PreSignup User", null));

        // Assert: domain user missing
        profile.ShouldBeFailure(UserErrors.UserNotFound(Guid.Empty).Code);
    }
}

