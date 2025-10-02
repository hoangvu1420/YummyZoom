using YummyZoom.Application.Auth.Commands.CompleteSignup;
using YummyZoom.Application.Auth.Commands.RequestPhoneOtp;
using YummyZoom.Application.Auth.Commands.VerifyPhoneOtp;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Users.Commands.CompleteProfile;
using YummyZoom.Application.Users.Commands.UpsertPrimaryAddress;
using YummyZoom.Application.Users.Queries.GetMyProfile;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.FunctionalTests.Features.Users;

using static Testing;

public class PhoneOtpProfileTests : BaseTestFixture
{
    private const string PhoneNumber = "+15550100001";

    [SetUp]
    public async Task EnsureUserRoleAsync()
    {
        await EnsureRolesExistAsync(Roles.User);
    }

    [Test]
    public async Task PhoneOtpFlow_ShouldPopulateProfileAndAddress()
    {
        // Request OTP for the phone number
        var otpRequest = await SendAsync(new RequestPhoneOtpCommand(PhoneNumber));
        otpRequest.ShouldBeSuccessful();
        var code = otpRequest.Value.Code;

        // Verify OTP and retrieve the identity user id
        var verifyResult = await SendAsync(new VerifyPhoneOtpCommand(PhoneNumber, code));
        verifyResult.ShouldBeSuccessful();
        var identityUserId = verifyResult.Value.IdentityUserId;

        // Simulate authenticated context for subsequent commands
        SetUserId(identityUserId);

        // New: Complete initial signup to create the domain user
        var signupResult = await SendAsync(new CompleteSignupCommand(
            Name: "Phone OTP User",
            Email: "phone.otp.user@example.com"));
        signupResult.ShouldBeSuccessful();

        // Upsert the primary address
        var addressResult = await SendAsync(new UpsertPrimaryAddressCommand(
            Street: "123 Main St",
            City: "Metropolis",
            State: "NY",
            ZipCode: "10101",
            Country: "US",
            Label: "Home",
            DeliveryInstructions: "Leave at front desk"));
        addressResult.ShouldBeSuccessful();
        var addressId = addressResult.Value;

        // Fetch the aggregated profile view
        var meResult = await SendAsync(new GetMyProfileQuery());
        meResult.ShouldBeSuccessful();
        var profile = meResult.Value;

        profile.UserId.Should().Be(identityUserId);
        profile.Name.Should().Be("Phone OTP User");
        profile.Email.Should().Be("phone.otp.user@example.com");
        profile.PhoneNumber.Should().Be(PhoneNumber);
        profile.Address.Should().NotBeNull();
        profile.Address!.AddressId.Should().Be(addressId);
        profile.Address.Street.Should().Be("123 Main St");
        profile.Address.City.Should().Be("Metropolis");
        profile.Address.State.Should().Be("NY");
        profile.Address.ZipCode.Should().Be("10101");
        profile.Address.Country.Should().Be("US");
        profile.Address.Label.Should().Be("Home");
        profile.Address.DeliveryInstructions.Should().Be("Leave at front desk");
    }
}
