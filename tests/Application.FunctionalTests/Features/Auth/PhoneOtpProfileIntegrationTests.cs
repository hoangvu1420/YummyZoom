using YummyZoom.Application.Auth.Commands.CompleteSignup;
using YummyZoom.Application.Auth.Commands.RequestPhoneOtp;
using YummyZoom.Application.Auth.Commands.VerifyPhoneOtp;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Users.Commands.UpsertPrimaryAddress;
using YummyZoom.Application.Users.Queries.GetMyProfile;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.FunctionalTests.Features.Auth;

using static Testing;

public class PhoneOtpProfileIntegrationTests : BaseTestFixture
{
    private const string PhoneNumber = "+15550110004";

    [SetUp]
    public async Task EnsureUserRoleAsync()
    {
        await EnsureRolesExistAsync(Roles.User);
    }

    [Test]
    public async Task NewUser_EndToEnd_SignupThenProfileAndAddress_ShouldSucceed()
    {
        // Arrange: OTP
        var otp = await SendAsync(new RequestPhoneOtpCommand(PhoneNumber));
        otp.ShouldBeSuccessful();
        var code = otp.Value.Code;

        // Act: verify → complete signup
        var verify = await SendAsync(new VerifyPhoneOtpCommand(PhoneNumber, code));
        verify.ShouldBeSuccessful();
        var identityUserId = verify.Value.IdentityUserId;
        SetUserId(identityUserId);

        var signup = await SendAsync(new CompleteSignupCommand("Phone OTP User", "phone.otp.user@example.com"));
        signup.ShouldBeSuccessful();

        // Address
        var address = await SendAsync(new UpsertPrimaryAddressCommand(
            Street: "123 Main St",
            City: "Metropolis",
            State: "NY",
            ZipCode: "10101",
            Country: "US",
            Label: "Home",
            DeliveryInstructions: "Leave at front desk"));
        address.ShouldBeSuccessful();

        // Assert profile aggregate view
        var me = await SendAsync(new GetMyProfileQuery());
        me.ShouldBeSuccessful();
        var profile = me.Value;

        profile.UserId.Should().Be(identityUserId);
        profile.Name.Should().Be("Phone OTP User");
        profile.Email.Should().Be("phone.otp.user@example.com");
        profile.PhoneNumber.Should().Be(PhoneNumber);
        profile.Address.Should().NotBeNull();
        profile.Address!.Street.Should().Be("123 Main St");
        profile.Address.City.Should().Be("Metropolis");
        profile.Address.State.Should().Be("NY");
        profile.Address.ZipCode.Should().Be("10101");
        profile.Address.Country.Should().Be("US");
        profile.Address.Label.Should().Be("Home");
        profile.Address.DeliveryInstructions.Should().Be("Leave at front desk");
    }
}

