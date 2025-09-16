using YummyZoom.Application.Auth.Commands.RequestPhoneOtp;
using YummyZoom.Application.Auth.Commands.VerifyPhoneOtp;
using YummyZoom.Application.Auth.Commands.CompleteSignup;
using YummyZoom.Application.Users.Queries.GetMyProfile;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.FunctionalTests.Features.Auth;

using static Testing;

public class PhoneOtpSignupFlowTests : BaseTestFixture
{
    private const string PhoneNumber = "+15550110001";

    [SetUp]
    public async Task EnsureUserRoleAsync()
    {
        await EnsureRolesExistAsync(Roles.User);
    }

    [Test]
    public async Task VerifyPhoneOtp_NewIdentityUser_ShouldRequireOnboarding()
    {
        // Arrange: request OTP
        var otpRequest = await SendAsync(new RequestPhoneOtpCommand(PhoneNumber));
        otpRequest.ShouldBeSuccessful();
        var code = otpRequest.Value.Code;

        // Act: verify OTP
        var verifyResult = await SendAsync(new VerifyPhoneOtpCommand(PhoneNumber, code));

        // Assert: indicates onboarding required
        verifyResult.ShouldBeSuccessful();
        verifyResult.Value.IsNewUser.Should().BeTrue();
        verifyResult.Value.RequiresOnboarding.Should().BeTrue();
    }

    [Test]
    public async Task CompleteSignup_AfterOtp_ShouldCreateDomainUser()
    {
        // Arrange: OTP roundtrip
        var otpRequest = await SendAsync(new RequestPhoneOtpCommand(PhoneNumber));
        otpRequest.ShouldBeSuccessful();
        var code = otpRequest.Value.Code;

        var verifyResult = await SendAsync(new VerifyPhoneOtpCommand(PhoneNumber, code));
        verifyResult.ShouldBeSuccessful();
        var identityUserId = verifyResult.Value.IdentityUserId;

        // Simulate authenticated context
        SetUserId(identityUserId);

        // Act: complete signup
        var complete = await SendAsync(new CompleteSignupCommand(
            Name: "OTP New User",
            Email: "otp.new.user@example.com"));

        // Assert: success and profile exists
        complete.ShouldBeSuccessful();
        var me = await SendAsync(new GetMyProfileQuery());
        me.ShouldBeSuccessful();
        me.Value.UserId.Should().Be(identityUserId);
        me.Value.Name.Should().Be("OTP New User");
        me.Value.Email.Should().Be("otp.new.user@example.com");
        me.Value.PhoneNumber.Should().Be(PhoneNumber);
    }

    [Test]
    public async Task CompleteSignup_Twice_ShouldBeIdempotent()
    {
        // Arrange: first completion
        var otpRequest = await SendAsync(new RequestPhoneOtpCommand(PhoneNumber));
        otpRequest.ShouldBeSuccessful();
        var code = otpRequest.Value.Code;

        var verifyResult = await SendAsync(new VerifyPhoneOtpCommand(PhoneNumber, code));
        verifyResult.ShouldBeSuccessful();
        var identityUserId = verifyResult.Value.IdentityUserId;
        SetUserId(identityUserId);

        var first = await SendAsync(new CompleteSignupCommand(
            Name: "OTP User",
            Email: "otp.user@example.com"));
        first.ShouldBeSuccessful();

        // Act: second completion (no-op)
        var second = await SendAsync(new CompleteSignupCommand(
            Name: "OTP User",
            Email: "otp.user@example.com"));

        // Assert: still succeeds; profile intact
        second.ShouldBeSuccessful();
        var me = await SendAsync(new GetMyProfileQuery());
        me.ShouldBeSuccessful();
        me.Value.UserId.Should().Be(identityUserId);
        me.Value.Name.Should().Be("OTP User");
        me.Value.Email.Should().Be("otp.user@example.com");
        me.Value.PhoneNumber.Should().Be(PhoneNumber);
    }
}

