using YummyZoom.Application.Auth.Commands.RequestPhoneOtp;
using YummyZoom.Application.Auth.Commands.VerifyPhoneOtp;
using YummyZoom.Application.Auth.Commands.CompleteSignup;
using YummyZoom.Application.Users.Queries.GetMyProfile;
using YummyZoom.Application.Users.Commands.CompleteProfile;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.FunctionalTests.Features.Auth;

using static Testing;

public class PhoneOtpReturningUserLoginTests : BaseTestFixture
{
    private const string PhoneNumber = "+15550110002";

    [SetUp]
    public async Task EnsureUserRoleAsync()
    {
        await EnsureRolesExistAsync(Roles.User);
    }

    private async Task<Guid> SignupOnceAsync()
    {
        var otp1 = await SendAsync(new RequestPhoneOtpCommand(PhoneNumber));
        otp1.ShouldBeSuccessful();
        var code1 = otp1.Value.Code;

        var verify1 = await SendAsync(new VerifyPhoneOtpCommand(PhoneNumber, code1));
        verify1.ShouldBeSuccessful();
        var identityUserId = verify1.Value.IdentityUserId;
        SetUserId(identityUserId);

        var complete = await SendAsync(new CompleteSignupCommand("Returning User", "returning.user@example.com"));
        complete.ShouldBeSuccessful();
        return identityUserId;
    }

    [Test]
    public async Task VerifyPhoneOtp_ExistingDomainUser_ShouldNotRequireOnboarding()
    {
        // Arrange: user already signed up
        await SignupOnceAsync();

        // Act: new OTP verify
        var otp2 = await SendAsync(new RequestPhoneOtpCommand(PhoneNumber));
        otp2.ShouldBeSuccessful();
        var code2 = otp2.Value.Code;

        var verify2 = await SendAsync(new VerifyPhoneOtpCommand(PhoneNumber, code2));

        // Assert: no onboarding required
        verify2.ShouldBeSuccessful();
        verify2.Value.IsNewUser.Should().BeFalse();
        verify2.Value.RequiresOnboarding.Should().BeFalse();
    }

    [Test]
    public async Task ReturningUser_CanAccessProfileEndpoints_WithoutReSignup()
    {
        // Arrange: user signed up once
        var identityUserId = await SignupOnceAsync();

        // Act: update profile and fetch me
        var update = await SendAsync(new CompleteProfileCommand("Returning User Updated", "updated@example.com"));
        update.ShouldBeSuccessful();

        var me = await SendAsync(new GetMyProfileQuery());

        // Assert
        me.ShouldBeSuccessful();
        me.Value.UserId.Should().Be(identityUserId);
        me.Value.Name.Should().Be("Returning User Updated");
        me.Value.Email.Should().Be("updated@example.com");
        me.Value.PhoneNumber.Should().Be(PhoneNumber);
    }
}

