using YummyZoom.Application.RoleAssignments.Commands.DeleteRoleAssignment;
using YummyZoom.Domain.RoleAssignmentAggregate;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.Infrastructure.Identity;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Interfaces.IRepositories;

namespace YummyZoom.Application.FunctionalTests.Authorization;

using static Testing;

/// <summary>
/// Integration tests for the complete claims-based authorization flow.
/// These tests verify that role assignments are properly converted to claims
/// and used for authorization decisions in the complete system integration.
/// </summary>
public class ClaimsBasedAuthorizationIntegrationTests : BaseTestFixture
{
    private Guid _restaurantId1;
    private Guid _restaurantId2;
    private Guid _restaurantId3;

    [SetUp]
    public async Task SetUp()
    {
        await SetupForAuthorizationTestsAsync();
        
        // Create test restaurants
        _restaurantId1 = Guid.NewGuid();
        _restaurantId2 = Guid.NewGuid();
        _restaurantId3 = Guid.NewGuid();
    }

    #region Claims Generation Tests

    [Test]
    public async Task UserWithMultipleRestaurantRoles_ShouldHaveCorrectPermissions()
    {
        // Arrange
        var userId = await RunAsUserWithMultipleRestaurantRolesAsync("multi@test.com", new[]
        {
            (_restaurantId1, RestaurantRole.Owner),
            (_restaurantId2, RestaurantRole.Staff),
            (_restaurantId3, RestaurantRole.Owner)
        });

        // Act & Assert - Test all restaurant permissions
        // Restaurant 1 - Owner permissions
        var ownerCommand1 = new TestRestaurantOwnerCommand(_restaurantId1);
        var staffCommand1 = new TestRestaurantStaffCommand(_restaurantId1);
        
        var result1 = await SendAsync(ownerCommand1);
        result1.ShouldBeSuccessful();
        
        var result2 = await SendAsync(staffCommand1);
        result2.ShouldBeSuccessful();

        // Restaurant 2 - Staff permissions only
        var ownerCommand2 = new TestRestaurantOwnerCommand(_restaurantId2);
        var staffCommand2 = new TestRestaurantStaffCommand(_restaurantId2);
        
        Func<Task> act1 = () => SendAsync(ownerCommand2);
        await act1.Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ForbiddenAccessException>();
        
        var result3 = await SendAsync(staffCommand2);
        result3.ShouldBeSuccessful();

        // Restaurant 3 - Owner permissions
        var ownerCommand3 = new TestRestaurantOwnerCommand(_restaurantId3);
        var staffCommand3 = new TestRestaurantStaffCommand(_restaurantId3);
        
        var result4 = await SendAsync(ownerCommand3);
        result4.ShouldBeSuccessful();
        
        var result5 = await SendAsync(staffCommand3);
        result5.ShouldBeSuccessful();
    }

    [Test]
    public async Task UserWithNoRestaurantRoles_ShouldHaveNoRestaurantPermissions()
    {
        // Arrange
        var userId = await RunAsDefaultUserAsync();
        
        // Act & Assert - Should fail for all restaurant operations
        var ownerCommand = new TestRestaurantOwnerCommand(_restaurantId1);
        var staffCommand = new TestRestaurantStaffCommand(_restaurantId1);

        Func<Task> act1 = () => SendAsync(ownerCommand);
        await act1.Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ForbiddenAccessException>();

        Func<Task> act2 = () => SendAsync(staffCommand);
        await act2.Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ForbiddenAccessException>();
    }

    [Test]
    public async Task UserRoleAssignmentChanges_ShouldUpdatePermissions()
    {
        // Arrange - Start with no restaurant permissions
        var userId = await RunAsDefaultUserAsync();
        var command = new TestRestaurantOwnerCommand(_restaurantId1);

        // Verify initial state - should fail
        Func<Task> actBefore = () => SendAsync(command);
        await actBefore.Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ForbiddenAccessException>();

        // Act - Add owner role as admin
        await RunAsAdministratorAsync();
        var roleAssignmentId = await CreateRoleAssignmentAsync(userId, _restaurantId1, RestaurantRole.Owner);

        // Switch back to user and refresh claims from database
        SetUserId(userId);
        await RefreshUserClaimsAsync();
        var result1 = await SendAsync(command);
        result1.ShouldBeSuccessful();

        // Remove role assignment as admin
        await RunAsAdministratorAsync();
        var deleteCommand = new DeleteRoleAssignmentCommand(roleAssignmentId);
        var deleteResult = await SendAsync(deleteCommand);
        deleteResult.ShouldBeSuccessful();

        // Switch back to user and refresh claims from database
        SetUserId(userId);
        await RefreshUserClaimsAsync();
        Func<Task> actAfter = () => SendAsync(command);
        await actAfter.Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ForbiddenAccessException>();
    }

    #endregion

    #region Claims Principal Factory Integration Tests

    [Test]
    public async Task ClaimsPrincipalFactory_ShouldGenerateCorrectClaims()
    {
        // Arrange
        var userId = await RunAsUserWithMultipleRestaurantRolesAsync("claims@test.com", new[]
        {
            (_restaurantId1, RestaurantRole.Owner),
            (_restaurantId2, RestaurantRole.Staff)
        });

        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var claimsFactory = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<ApplicationUser>>();

        // Act - Generate claims principal
        var user = await userManager.FindByIdAsync(userId.ToString());
        user.Should().NotBeNull();
        
        var principal = await claimsFactory.CreateAsync(user!);

        // Assert - Verify permission claims are present
        var permissionClaims = principal.Claims.Where(c => c.Type == "permission").ToList();
        permissionClaims.Should().HaveCount(3, "User should have 2 restaurant role assignments + 1 user ownership claim");

        permissionClaims.Should().Contain(c => c.Value == $"RestaurantOwner:{_restaurantId1}");
        permissionClaims.Should().Contain(c => c.Value == $"RestaurantStaff:{_restaurantId2}");
        permissionClaims.Should().Contain(c => c.Value == $"UserOwner:{userId}");
    }

    #endregion

    #region End-to-End Integration Tests

    [Test]
    public async Task EndToEnd_CompleteAuthorizationFlow_ShouldWork()
    {
        // This test verifies the complete flow:
        // 1. Create user and role assignments
        // 2. Claims are generated from role assignments
        // 3. Authorization policies use claims for decisions
        // 4. MediatR pipeline enforces authorization

        // Arrange - Create a complex scenario
        var userId = await RunAsUserWithMultipleRestaurantRolesAsync("e2e@test.com", new[]
        {
            (_restaurantId1, RestaurantRole.Owner),
            (_restaurantId2, RestaurantRole.Staff)
        });

        // Verify role assignments were created in database
        var roleAssignments = await GetRoleAssignmentsForUser(userId);
        roleAssignments.Should().HaveCount(2);

        // Act & Assert - Test authorization through complete pipeline
        
        // Test 1: Owner command on restaurant 1 (should succeed)
        var ownerCommand1 = new TestRestaurantOwnerCommand(_restaurantId1);
        var result1 = await SendAsync(ownerCommand1);
        result1.ShouldBeSuccessful();

        // Test 2: Staff command on restaurant 1 (should succeed - owners can do staff actions)
        var staffCommand1 = new TestRestaurantStaffCommand(_restaurantId1);
        var result2 = await SendAsync(staffCommand1);
        result2.ShouldBeSuccessful();

        // Test 3: Owner command on restaurant 2 (should fail - user is only staff)
        var ownerCommand2 = new TestRestaurantOwnerCommand(_restaurantId2);
        Func<Task> act1 = () => SendAsync(ownerCommand2);
        await act1.Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ForbiddenAccessException>();

        // Test 4: Staff command on restaurant 2 (should succeed)
        var staffCommand2 = new TestRestaurantStaffCommand(_restaurantId2);
        var result3 = await SendAsync(staffCommand2);
        result3.ShouldBeSuccessful();

        // Test 5: Command on restaurant 3 (should fail - no permissions)
        var ownerCommand3 = new TestRestaurantOwnerCommand(_restaurantId3);
        Func<Task> act2 = () => SendAsync(ownerCommand3);
        await act2.Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ForbiddenAccessException>();
    }

    #endregion

    #region Helper Methods

    private async Task<List<RoleAssignment>> GetRoleAssignmentsForUser(Guid userId)
    {
        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRoleAssignmentRepository>();
        var domainUserId = YummyZoom.Domain.UserAggregate.ValueObjects.UserId.Create(userId);
        
        return await repository.GetByUserIdAsync(domainUserId);
    }

    #endregion
} 
