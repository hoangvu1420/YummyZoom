using FluentAssertions;
using YummyZoom.Application.Auth.Commands.CompleteSignup;
using YummyZoom.Application.Auth.Commands.RequestPhoneOtp;
using YummyZoom.Application.Auth.Commands.VerifyPhoneOtp;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.Users.Queries.GetMyProfile;
using YummyZoom.Infrastructure.Identity;
using YummyZoom.SharedKernel.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace YummyZoom.Application.FunctionalTests.Features.Auth;

using static Testing;

/// <summary>
/// Test that reproduces the exact production scenario reported by the user.
/// This test verifies that the CompleteSignup command correctly updates both
/// the domain user and Identity user when called multiple times.
/// </summary>
public class ProductionScenarioReproductionTests : BaseTestFixture
{
    private const string ProductionPhoneNumber = "+840354088526";
    
    [SetUp]
    public async Task EnsureUserRoleAsync()
    {
        await EnsureRolesExistAsync(Roles.User);
    }

    [Test]
    public async Task ReproduceProductionIssue_UserDataShouldBeUpdated()
    {
        // This test reproduces the exact scenario from production logs:
        // 1. OTP verification creates Identity user with temp email
        // 2. CompleteSignup is called with "hoangvu" and "hoangnguyenvu2222@gmail.com"
        // 3. User data should be updated in both domain and Identity tables

        // Step 1: OTP Request (matches log: "YummyZoom Request: VerifyPhoneOtpCommand")
        var otpRequest = await SendAsync(new RequestPhoneOtpCommand(ProductionPhoneNumber));
        otpRequest.ShouldBeSuccessful();
        var code = otpRequest.Value.Code;

        // Step 2: OTP Verification (matches log: "VerifyPhoneOtpCommand { PhoneNumber = +840354088526, Code = 111111 }")
        var verifyResult = await SendAsync(new VerifyPhoneOtpCommand(ProductionPhoneNumber, "111111"));
        verifyResult.ShouldBeSuccessful();
        
        var identityUserId = verifyResult.Value.IdentityUserId;
        SetUserId(identityUserId);

        // Verify initial state: Identity user should have temp email
        await TestDatabaseManager.ExecuteInScopeAsync(async dbContext =>
        {
            var initialIdentityUser = await dbContext.Users.FindAsync(identityUserId);
            initialIdentityUser.Should().NotBeNull();
            initialIdentityUser!.Email.Should().EndWith("@phone.temp"); // Temp email pattern
            initialIdentityUser.UserName.Should().Be(ProductionPhoneNumber);
            initialIdentityUser.PhoneNumber.Should().Be(ProductionPhoneNumber);
        });

        // Step 3: Complete Signup (matches log: "CompleteSignupCommand { Name = hoangvu, Email = hoangnguyenvu2222@gmail.com }")
        var completeSignup = await SendAsync(new CompleteSignupCommand("hoangvu", "hoangnguyenvu2222@gmail.com"));
        completeSignup.ShouldBeSuccessful();

        // Step 4: Verify the user data is updated correctly
        await AssertCompleteUserData(identityUserId, "hoangvu", "hoangnguyenvu2222@gmail.com", ProductionPhoneNumber);
    }

    [Test]
    public async Task ReproduceProductionIssue_MultipleCompleteSignupCalls()
    {
        // This reproduces the scenario where CompleteSignup might be called multiple times
        // (due to network retries, user clicking multiple times, etc.)
        
        // Initial OTP flow
        var (identityUserId, _) = await CompleteOtpFlowAsync();
        SetUserId(identityUserId);

        // Simulate multiple CompleteSignup calls (production scenario)
        // First call might have been with incomplete/default data
        var firstAttempt = await SendAsync(new CompleteSignupCommand("temp", null));
        firstAttempt.ShouldBeSuccessful();

        // Second call with the actual user data (what should be preserved)
        var secondAttempt = await SendAsync(new CompleteSignupCommand("hoangvu", "hoangnguyenvu2222@gmail.com"));
        secondAttempt.ShouldBeSuccessful();

        // Third call with same data (idempotency test)
        var thirdAttempt = await SendAsync(new CompleteSignupCommand("hoangvu", "hoangnguyenvu2222@gmail.com"));
        thirdAttempt.ShouldBeSuccessful();

        // Final verification: should have the data from the second call
        await AssertCompleteUserData(identityUserId, "hoangvu", "hoangnguyenvu2222@gmail.com", ProductionPhoneNumber);
    }

    [Test]
    public async Task VerifyBothDomainAndIdentityUsersAreUpdated()
    {
        // This test specifically verifies that BOTH the domain user and Identity user
        // are updated when CompleteSignup is called, addressing the root cause of the issue

        var (identityUserId, _) = await CompleteOtpFlowAsync();
        SetUserId(identityUserId);

        // Get initial Identity user state (should have temp email)
        string initialTempEmail = "";
        await TestDatabaseManager.ExecuteInScopeAsync(async dbContext =>
        {
            var initialIdentityUser = await dbContext.Users.FindAsync(identityUserId);
            initialIdentityUser.Should().NotBeNull();
            initialTempEmail = initialIdentityUser!.Email!;
            initialTempEmail.Should().EndWith("@phone.temp");
        });

        // Complete signup with real data
        var completeSignup = await SendAsync(new CompleteSignupCommand("hoangvu", "hoangnguyenvu2222@gmail.com"));
        completeSignup.ShouldBeSuccessful();

        // Verify domain user is updated (via GetMyProfile)
        var profile = await SendAsync(new GetMyProfileQuery());
        profile.ShouldBeSuccessful();
        profile.Value.Name.Should().Be("hoangvu");
        profile.Value.Email.Should().Be("hoangnguyenvu2222@gmail.com");
        profile.Value.PhoneNumber.Should().Be(ProductionPhoneNumber);

        // Verify Identity user is ALSO updated (this was the missing piece)
        await TestDatabaseManager.ExecuteInScopeAsync(async dbContext =>
        {
            var updatedIdentityUser = await dbContext.Users.FindAsync(identityUserId);
            updatedIdentityUser.Should().NotBeNull();
            updatedIdentityUser!.Email.Should().Be("hoangnguyenvu2222@gmail.com");
            updatedIdentityUser.Email.Should().NotBe(initialTempEmail); // Should be different from temp email
            updatedIdentityUser.UserName.Should().Be(ProductionPhoneNumber); // Username should remain as phone number for login
            updatedIdentityUser.PhoneNumber.Should().Be(ProductionPhoneNumber);
        });
    }

    #region Helper Methods

    private async Task<(Guid identityUserId, string code)> CompleteOtpFlowAsync()
    {
        var otpRequest = await SendAsync(new RequestPhoneOtpCommand(ProductionPhoneNumber));
        otpRequest.ShouldBeSuccessful();
        var code = otpRequest.Value.Code;

        var verifyResult = await SendAsync(new VerifyPhoneOtpCommand(ProductionPhoneNumber, code));
        verifyResult.ShouldBeSuccessful();
        
        return (verifyResult.Value.IdentityUserId, code);
    }

    private async Task AssertCompleteUserData(Guid identityUserId, string expectedName, string expectedEmail, string expectedPhone)
    {
        // Check domain user
        var profile = await SendAsync(new GetMyProfileQuery());
        profile.ShouldBeSuccessful();
        profile.Value.UserId.Should().Be(identityUserId);
        profile.Value.Name.Should().Be(expectedName);
        profile.Value.Email.Should().Be(expectedEmail);
        profile.Value.PhoneNumber.Should().Be(expectedPhone);

        // Check Identity user
        await TestDatabaseManager.ExecuteInScopeAsync(async dbContext =>
        {
            var identityUser = await dbContext.Users.FindAsync(identityUserId);
            identityUser.Should().NotBeNull();
            identityUser!.Email.Should().Be(expectedEmail);
            identityUser.UserName.Should().Be(expectedPhone); // Username should remain as phone number for login
            identityUser.PhoneNumber.Should().Be(expectedPhone);
        });
    }

    private static T GetService<T>() where T : notnull => Testing.GetService<T>();

    #endregion
}
