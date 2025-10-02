using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Infrastructure.Identity;

namespace YummyZoom.Application.FunctionalTests.Authorization;

using static Testing;

[TestFixture]
public class UserCommandAuthorizationTests : BaseTestFixture
{
    private Guid _testUserId1;
    private Guid _testUserId2;

    [SetUp]
    public async Task SetUp()
    {
        await ResetState();
        await SetupForAuthorizationTestsAsync();

        // Create test user IDs for testing
        _testUserId1 = Guid.NewGuid();
        _testUserId2 = Guid.NewGuid();
    }

    #region User Owner Policy Tests

    [Test]
    public async Task UserOwnerCommand_WithCurrentUser_ShouldSucceed()
    {
        // Arrange - User accessing their own data
        var userId = await RunAsDefaultUserAsync();
        var command = new TestUserOwnerCommand(userId);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    [Test]
    public async Task UserOwnerCommand_WithDifferentUser_ShouldFail()
    {
        // Arrange - User trying to access someone else's data
        await RunAsDefaultUserAsync();
        var command = new TestUserOwnerCommand(_testUserId1); // Different user ID

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task UserOwnerCommand_WithAdministrator_ShouldSucceed()
    {
        // Arrange - Admin should be able to access any user's data
        await RunAsAdministratorAsync();
        var command = new TestUserOwnerCommand(_testUserId1);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    [Test]
    public async Task UserOwnerCommand_WithoutAuthentication_ShouldFail()
    {
        // Arrange - No user authentication
        var command = new TestUserOwnerCommand(_testUserId1);

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    #endregion

    #region Unprotected User Command Tests

    [Test]
    public async Task UnprotectedUserCommand_WithoutAuthentication_ShouldSucceed()
    {
        // Arrange - No authentication required for unprotected commands
        var command = new TestUnprotectedUserCommand(_testUserId1);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    [Test]
    public async Task UnprotectedUserCommand_WithAuthentication_ShouldSucceed()
    {
        // Arrange - Authenticated user should also be able to call unprotected commands
        await RunAsDefaultUserAsync();
        var command = new TestUnprotectedUserCommand(_testUserId1);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    #endregion

    #region User Claims Verification Tests

    [Test]
    public async Task UserClaims_ShouldBeGeneratedCorrectly()
    {
        // Arrange
        var userId = await RunAsDefaultUserAsync();

        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var claimsFactory = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<ApplicationUser>>();

        // Act - Generate claims principal
        var user = await userManager.FindByIdAsync(userId.ToString());
        user.Should().NotBeNull();

        var principal = await claimsFactory.CreateAsync(user!);

        // Assert - Verify user permission claims are present
        var permissionClaims = principal.Claims.Where(c => c.Type == "permission").ToList();

        // Should have at least the user's own permission
        permissionClaims.Should().Contain(c => c.Value == $"UserOwner:{userId}",
            "Every user should have UserOwner permission for their own ID");
    }

    [Test]
    public async Task AdminUserClaims_ShouldIncludeAdminPermissions()
    {
        // Arrange
        var adminUserId = await RunAsAdministratorAsync();

        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var claimsFactory = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<ApplicationUser>>();

        // Act - Generate claims principal for admin
        var adminUser = await userManager.FindByIdAsync(adminUserId.ToString());
        adminUser.Should().NotBeNull();

        var principal = await claimsFactory.CreateAsync(adminUser!);

        // Assert - Verify admin permission claims are present
        var permissionClaims = principal.Claims.Where(c => c.Type == "permission").ToList();

        // Should have both user self-permission and admin permission
        permissionClaims.Should().Contain(c => c.Value == $"UserOwner:{adminUserId}",
            "Admin should have UserOwner permission for their own ID");
        permissionClaims.Should().Contain(c => c.Value == "UserAdmin:*",
            "Admin should have UserAdmin:* permission");
    }

    #endregion
}
