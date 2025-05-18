using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.Infrastructure.Identity;
using YummyZoom.Application.Users.Commands.AssignRoleToUser;
using YummyZoom.Application.Users.Commands.RemoveRoleFromUser;
using YummyZoom.SharedKernel.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Users.Commands.RegisterUser;

namespace YummyZoom.Application.FunctionalTests.Users;

using static Testing;

public class RoleAssignmentTests : BaseTestFixture
{
    private Guid _administratorUserId;
    private Guid _testUserId;

    [SetUp]
    public async Task TestSetup()
    {
        await ResetState();
        await SetupForUserRegistrationTestsAsync(); 
        _administratorUserId = await RunAsAdministratorAsync(); 
        
        // Create a test user to assign/remove roles from
        var registerUserCommand = new RegisterUserCommand
        {
            Name = "Test User",
            Email = "test.user@example.com",
            Password = "Password123!"
        };
        var registerResult = await SendAsync(registerUserCommand);

        registerResult.ShouldBeSuccessful(); 
        _testUserId = registerResult.Value;

        // Ensure the test user is initially only in the Customer role (default)
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUser = await userManager.FindByIdAsync(_testUserId.ToString()); 
        appUser.Should().NotBeNull("The test user should be found after registration."); 
        var roles = await userManager.GetRolesAsync(appUser!); 
        roles.Should().Contain(Roles.Customer);
        roles.Count.Should().Be(1);
    }

    [Test]
    public async Task AssignRoleToUser_WithValidData_ShouldSucceedAndAssignRole()
    {
        // Arrange
        var command = new AssignRoleToUserCommand(
            UserId: _testUserId,
            RoleName: Roles.RestaurantOwner,
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        // Verify the role assignment is added to the Domain UserAggregate
        var domainUser = await FindAsync<User>(UserId.Create(_testUserId));
        domainUser.Should().NotBeNull();
        domainUser!.UserRoles.Should().ContainSingle(ra => ra.RoleName == Roles.RestaurantOwner);
        
        // Verify the role is assigned in Identity
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUser = await userManager.FindByIdAsync(_testUserId.ToString());
        appUser.Should().NotBeNull();
        var isInRole = await userManager.IsInRoleAsync(appUser!, Roles.RestaurantOwner);
        isInRole.Should().BeTrue("User should be assigned to the RestaurantOwner role in Identity.");
    }

    [Test]
    public async Task AssignRoleToUser_WhenUserNotFound_ShouldReturnFailureResult()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var command = new AssignRoleToUserCommand(
            UserId: nonExistentUserId,
            RoleName: Roles.RestaurantOwner,
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.UserNotFound(nonExistentUserId).Code);
    }

    [Test]
    public async Task AssignRoleToUser_WithInvalidRoleName_ShouldReturnFailureResult()
    {
        // Arrange
        var invalidRoleName = "InvalidRole";
        var command = new AssignRoleToUserCommand(
            UserId: _testUserId,
            RoleName: invalidRoleName,
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task AssignRoleToUser_WhenRoleAlreadyAssigned_ShouldSucceedAndNotDuplicate()
    {
        // Arrange
        // User is already in Customer role from setup
        var command = new AssignRoleToUserCommand(
            UserId: _testUserId,
            RoleName: Roles.Customer,
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful(); 

        // Verify the role is still assigned in Identity (no change expected)
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUser = await userManager.FindByIdAsync(_testUserId.ToString());
        appUser.Should().NotBeNull();
        var isInRole = await userManager.IsInRoleAsync(appUser!, Roles.Customer);
        isInRole.Should().BeTrue("User should still be in the Customer role in Identity.");

        // Verify the role assignment is still only one in the Domain UserAggregate
        var domainUser = await FindAsync<User>(UserId.Create(_testUserId));
        domainUser.Should().NotBeNull();
        domainUser!.UserRoles.Should().ContainSingle(ra => ra.RoleName == Roles.Customer);
    }

    [Test]
    public async Task AssignRoleToUser_AsNonAdministrator_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        await RunAsDefaultUserAsync(); // Run as a non-administrator user
        var command = new AssignRoleToUserCommand(
            UserId: _testUserId,
            RoleName: Roles.RestaurantOwner,
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task RemoveRoleFromUser_WithValidData_ShouldSucceedAndRemoveRole()
    {
        // Arrange
        // First assign a role to remove
        var assignCommand = new AssignRoleToUserCommand(
            UserId: _testUserId,
            RoleName: Roles.RestaurantOwner,
            TargetEntityId: null,
            TargetEntityType: null);
        var assignResult = await SendAsync(assignCommand);
        assignResult.ShouldBeSuccessful();

        // Verify role is assigned before removing
        using var scopeBeforeRemove = CreateScope();
        var userManagerBeforeRemove = scopeBeforeRemove.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUserBeforeRemove = await userManagerBeforeRemove.FindByIdAsync(_testUserId.ToString());
        (await userManagerBeforeRemove.IsInRoleAsync(appUserBeforeRemove!, Roles.RestaurantOwner)).Should().BeTrue(); 

        var command = new RemoveRoleFromUserCommand(
            UserId: _testUserId,
            RoleName: Roles.RestaurantOwner,
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        // Verify the role is removed in Identity
        using var scopeAfterRemove = CreateScope();
        var userManagerAfterRemove = scopeAfterRemove.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUserAfterRemove = await userManagerAfterRemove.FindByIdAsync(_testUserId.ToString()); 
        (await userManagerAfterRemove.IsInRoleAsync(appUserAfterRemove!, Roles.RestaurantOwner)).Should().BeFalse("User should be removed from the RestaurantOwner role in Identity."); 

        // Verify the role assignment is removed from the Domain UserAggregate
        var domainUser = await FindAsync<User>(UserId.Create(_testUserId));
        domainUser.Should().NotBeNull();
        domainUser!.UserRoles.Should().NotContain(ra => ra.RoleName == Roles.RestaurantOwner);
    }

    [Test]
    public async Task RemoveRoleFromUser_WhenUserNotFound_ShouldReturnFailureResult()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var command = new RemoveRoleFromUserCommand(
            UserId: nonExistentUserId,
            RoleName: Roles.Customer,
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.UserNotFound(nonExistentUserId).Code);
    }

    [Test]
    public async Task RemoveRoleFromUser_WhenRoleNotAssigned_ShouldReturnFailureResult()
    {
        // Arrange
        // User is only in Customer role from setup
        var command = new RemoveRoleFromUserCommand(
            UserId: _testUserId,
            RoleName: Roles.RestaurantOwner, // Role not assigned
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.RoleNotFound(Roles.RestaurantOwner).Code);
    }

    [Test]
    public async Task RemoveRoleFromUser_WhenRemovingLastRole_ShouldReturnFailureResult()
    {
        // Arrange
        // User is only in Customer role from setup
        var command = new RemoveRoleFromUserCommand(
            UserId: _testUserId,
            RoleName: Roles.Customer, // The only assigned role
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.CannotRemoveLastRole.Code);

        // Verify the role was NOT removed in Identity
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUser = await userManager.FindByIdAsync(_testUserId.ToString());
        appUser.Should().NotBeNull();
        var isInRole = await userManager.IsInRoleAsync(appUser!, Roles.Customer);
        isInRole.Should().BeTrue("User should still be in the Customer role in Identity.");

        // Verify the role assignment was NOT removed from the Domain UserAggregate
        var domainUser = await FindAsync<User>(UserId.Create(_testUserId));
        domainUser.Should().NotBeNull();
        domainUser!.UserRoles.Should().ContainSingle(ra => ra.RoleName == Roles.Customer);
    }

    [Test]
    public async Task RemoveRoleFromUser_AsNonAdministrator_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        // First assign a role as administrator
        var assignCommand = new AssignRoleToUserCommand(
            UserId: _testUserId,
            RoleName: Roles.RestaurantOwner,
            TargetEntityId: null,
            TargetEntityType: null);
        await SendAsync(assignCommand);

        await RunAsDefaultUserAsync(); // Run as a non-administrator user
        var command = new RemoveRoleFromUserCommand(
            UserId: _testUserId,
            RoleName: Roles.RestaurantOwner,
            TargetEntityId: null,
            TargetEntityType: null);

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
