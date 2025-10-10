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
/// Tests for CompleteSignupCommand idempotency and proper data updates.
/// These tests specifically reproduce the production scenario where:
/// 1. User does OTP verification (creates Identity user with temp email)
/// 2. User calls CompleteSignup multiple times with different data
/// 3. The latest provided data should be stored in both domain and identity users
/// </summary>
public class CompleteSignupIdempotencyTests : BaseTestFixture
{
    private const string TestPhoneNumber = "+840354088526";
    
    [SetUp]
    public async Task EnsureUserRoleAsync()
    {
        await EnsureRolesExistAsync(Roles.User);
    }

    [Test]
    public async Task CompleteSignup_CalledMultipleTimesWithSameData_ShouldBeIdempotent()
    {
        // Arrange: Complete OTP flow
        var (identityUserId, _) = await CompleteOtpFlowAsync();
        SetUserId(identityUserId);

        var signupData = new CompleteSignupCommand("Test User", "test@example.com");

        // Act: Call CompleteSignup multiple times with same data
        var first = await SendAsync(signupData);
        var second = await SendAsync(signupData);
        var third = await SendAsync(signupData);

        // Assert: All calls succeed
        first.ShouldBeSuccessful();
        second.ShouldBeSuccessful();
        third.ShouldBeSuccessful();

        // Verify final state
        await AssertUserDataAsync(identityUserId, "Test User", "test@example.com", TestPhoneNumber);
    }

    [Test]
    public async Task CompleteSignup_CalledMultipleTimesWithDifferentData_ShouldUseLatestData()
    {
        // Arrange: Complete OTP flow
        var (identityUserId, _) = await CompleteOtpFlowAsync();
        SetUserId(identityUserId);

        // Act: Call CompleteSignup multiple times with different data (production scenario)
        var first = await SendAsync(new CompleteSignupCommand("First Name", "first@example.com"));
        var second = await SendAsync(new CompleteSignupCommand("Second Name", "second@example.com"));
        var third = await SendAsync(new CompleteSignupCommand("hoangvu", "hoangnguyenvu2222@gmail.com"));

        // Assert: All calls succeed
        first.ShouldBeSuccessful();
        second.ShouldBeSuccessful();
        third.ShouldBeSuccessful();

        // Verify final state uses the LATEST data (third call)
        await AssertUserDataAsync(identityUserId, "hoangvu", "hoangnguyenvu2222@gmail.com", TestPhoneNumber);
    }

    [Test]
    public async Task CompleteSignup_WithNullEmail_ThenWithRealEmail_ShouldUpdateCorrectly()
    {
        // Arrange: Complete OTP flow
        var (identityUserId, _) = await CompleteOtpFlowAsync();
        SetUserId(identityUserId);

        // Act: First call with null email, then with real email
        var withoutEmail = await SendAsync(new CompleteSignupCommand("Test User", null));
        var withEmail = await SendAsync(new CompleteSignupCommand("Updated User", "real@example.com"));

        // Assert: Both calls succeed
        withoutEmail.ShouldBeSuccessful();
        withEmail.ShouldBeSuccessful();

        // Verify final state
        await AssertUserDataAsync(identityUserId, "Updated User", "real@example.com", TestPhoneNumber);
    }

    [Test]
    public async Task CompleteSignup_AfterPreviousIncompleteAttempt_ShouldUpdateBothIdentityAndDomain()
    {
        // Arrange: Simulate a previous incomplete signup attempt
        var (identityUserId, _) = await CompleteOtpFlowAsync();
        SetUserId(identityUserId);

        // First incomplete attempt (simulates what happens in production)
        var incomplete = await SendAsync(new CompleteSignupCommand("Temp Name", null));
        incomplete.ShouldBeSuccessful();

        // Act: Complete signup with real data (production scenario)
        var complete = await SendAsync(new CompleteSignupCommand("hoangvu", "hoangnguyenvu2222@gmail.com"));

        // Assert: Success
        complete.ShouldBeSuccessful();

        // Verify both domain and identity users are updated
        await AssertUserDataAsync(identityUserId, "hoangvu", "hoangnguyenvu2222@gmail.com", TestPhoneNumber);
    }

    [Test]
    public async Task CompleteSignup_RepeatedCallsWithEmailChanges_ShouldSyncIdentityUser()
    {
        // Arrange: Complete OTP flow
        var (identityUserId, _) = await CompleteOtpFlowAsync();
        SetUserId(identityUserId);

        // Act: Multiple calls with email changes
        var first = await SendAsync(new CompleteSignupCommand("User", "first@test.com"));
        var second = await SendAsync(new CompleteSignupCommand("User", "second@test.com"));
        var final = await SendAsync(new CompleteSignupCommand("Final User", "final@test.com"));

        // Assert: All succeed
        first.ShouldBeSuccessful();
        second.ShouldBeSuccessful();
        final.ShouldBeSuccessful();

        // Verify both domain and Identity user have the latest email
        await AssertUserDataAsync(identityUserId, "Final User", "final@test.com", TestPhoneNumber);
        
        // Specifically verify Identity user email
        await AssertIdentityUserEmailAsync(identityUserId, "final@test.com");
    }

    #region Helper Methods

    private async Task<(Guid identityUserId, string code)> CompleteOtpFlowAsync()
    {
        // Request OTP
        var otpRequest = await SendAsync(new RequestPhoneOtpCommand(TestPhoneNumber));
        otpRequest.ShouldBeSuccessful();
        var code = otpRequest.Value.Code;

        // Verify OTP
        var verifyResult = await SendAsync(new VerifyPhoneOtpCommand(TestPhoneNumber, code));
        verifyResult.ShouldBeSuccessful();
        
        return (verifyResult.Value.IdentityUserId, code);
    }

    private async Task AssertUserDataAsync(Guid identityUserId, string expectedName, string expectedEmail, string expectedPhone)
    {
        // Check domain user via GetMyProfile
        var profile = await SendAsync(new GetMyProfileQuery());
        profile.ShouldBeSuccessful();
        
        profile.Value.UserId.Should().Be(identityUserId);
        profile.Value.Name.Should().Be(expectedName);
        profile.Value.Email.Should().Be(expectedEmail);
        profile.Value.PhoneNumber.Should().Be(expectedPhone);

        // Also check Identity user
        await AssertIdentityUserDataAsync(identityUserId, expectedEmail, expectedPhone);
    }

    private async Task AssertIdentityUserDataAsync(Guid identityUserId, string expectedEmail, string expectedPhone)
    {
        await TestDatabaseManager.ExecuteInScopeAsync(async dbContext =>
        {
            var identityUser = await dbContext.Users.FindAsync(identityUserId);
            
            identityUser.Should().NotBeNull();
            identityUser!.Email.Should().Be(expectedEmail);
            identityUser.UserName.Should().Be(expectedPhone); // Username is phone in OTP flow
            identityUser.PhoneNumber.Should().Be(expectedPhone);
        });
    }

    private async Task AssertIdentityUserEmailAsync(Guid identityUserId, string expectedEmail)
    {
        await TestDatabaseManager.ExecuteInScopeAsync(async dbContext =>
        {
            var identityUser = await dbContext.Users.FindAsync(identityUserId);
            
            identityUser.Should().NotBeNull();
            identityUser!.Email.Should().Be(expectedEmail);
        });
    }

    private static T GetService<T>() where T : notnull => Testing.GetService<T>();

    #endregion
}
