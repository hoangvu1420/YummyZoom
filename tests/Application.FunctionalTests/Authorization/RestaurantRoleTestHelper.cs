using YummyZoom.Application.RoleAssignments.Commands.CreateRoleAssignment;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.Application.FunctionalTests.UserManagement;
using YummyZoom.SharedKernel.Constants;
using YummyZoom.Application.FunctionalTests.Infrastructure;

namespace YummyZoom.Application.FunctionalTests.Authorization;

/// <summary>
/// Helper class for managing restaurant-specific role assignments and scenarios in tests.
/// </summary>
public static class RestaurantRoleTestHelper
{
    /// <summary>
    /// Creates a role assignment for a user in a restaurant with the specified role.
    /// Requires an administrator to be logged in.
    /// </summary>
    public static async Task<Guid> CreateRoleAssignmentAsync(Guid userId, Guid restaurantId, RestaurantRole role)
    {
        var command = new CreateRoleAssignmentCommand(userId, restaurantId, role);
        var result = await Testing.SendAsync(command);
        
        if (result.IsFailure)
        {
            throw new Exception($"Failed to create role assignment: {result.Error.Description}");
        }
        
        return result.Value.RoleAssignmentId;
    }

    /// <summary>
    /// Sets up a user as a restaurant owner for the specified restaurant.
    /// Creates the user, assigns administrator role temporarily to create role assignment, then switches to the user.
    /// </summary>
    public static async Task<Guid> RunAsRestaurantOwnerAsync(string email, Guid restaurantId)
    {
        // First ensure we have admin access to create role assignments
        await TestUserManager.EnsureRolesExistAsync(Roles.Administrator);
        var adminUserId = await TestUserManager.RunAsAdministratorAsync();
        
        // Create the target user
        var userId = await TestUserManager.RunAsUserAsync(email, TestConfiguration.DefaultUsers.CommonTestPassword, Array.Empty<string>());
        
        // Switch back to admin to create role assignment
        await TestUserManager.RunAsUserAsync(TestConfiguration.DefaultUsers.Administrator.Email, TestConfiguration.DefaultUsers.Administrator.Password, new[] { Roles.Administrator });
        
        // Create the restaurant owner role assignment
        await CreateRoleAssignmentAsync(userId, restaurantId, RestaurantRole.Owner);
        
        // Switch back to the target user and add the restaurant owner claim
        TestUserManager.SetCurrentUserId(userId);
        
        // Add the restaurant owner permission claim
        TestAuthenticationService.AddPermissionClaim(Roles.RestaurantOwner, restaurantId.ToString());
        
        return userId;
    }

    /// <summary>
    /// Sets up a user as restaurant staff for the specified restaurant.
    /// Creates the user, assigns administrator role temporarily to create role assignment, then switches to the user.
    /// </summary>
    public static async Task<Guid> RunAsRestaurantStaffAsync(string email, Guid restaurantId)
    {
        // First ensure we have admin access to create role assignments
        await TestUserManager.EnsureRolesExistAsync(Roles.Administrator);
        var adminUserId = await TestUserManager.RunAsAdministratorAsync();
        
        // Create the target user
        var userId = await TestUserManager.RunAsUserAsync(email, TestConfiguration.DefaultUsers.CommonTestPassword, Array.Empty<string>());
        
        // Switch back to admin to create role assignment
        await TestUserManager.RunAsUserAsync(TestConfiguration.DefaultUsers.Administrator.Email, TestConfiguration.DefaultUsers.Administrator.Password, new[] { Roles.Administrator });
        
        // Create the restaurant staff role assignment
        await CreateRoleAssignmentAsync(userId, restaurantId, RestaurantRole.Staff);
        
        // Switch back to the target user and add the restaurant staff claim
        TestUserManager.SetCurrentUserId(userId);
        
        // Add the restaurant staff permission claim
        TestAuthenticationService.AddPermissionClaim(Roles.RestaurantStaff, restaurantId.ToString());
        
        return userId;
    }

    /// <summary>
    /// Sets up a user with multiple restaurant roles for testing complex authorization scenarios.
    /// </summary>
    public static async Task<Guid> RunAsUserWithMultipleRestaurantRolesAsync(string email, (Guid restaurantId, RestaurantRole role)[] roleAssignments)
    {
        // First ensure we have admin access to create role assignments
        await TestUserManager.EnsureRolesExistAsync(Roles.Administrator);
        var adminUserId = await TestUserManager.RunAsAdministratorAsync();
        
        // Create the target user
        var userId = await TestUserManager.RunAsUserAsync(email, TestConfiguration.DefaultUsers.CommonTestPassword, Array.Empty<string>());
        
        // Switch back to admin to create role assignments
        await TestUserManager.RunAsUserAsync(TestConfiguration.DefaultUsers.Administrator.Email, TestConfiguration.DefaultUsers.Administrator.Password, new[] { Roles.Administrator });
        
        // Create all role assignments
        foreach (var (restaurantId, role) in roleAssignments)
        {
            await CreateRoleAssignmentAsync(userId, restaurantId, role);
        }
        
        // Switch back to the target user and add all permission claims
        TestUserManager.SetCurrentUserId(userId);
        
        // Add all permission claims
        foreach (var (restaurantId, role) in roleAssignments)
        {
            var roleConstant = role switch
            {
                RestaurantRole.Owner => Roles.RestaurantOwner,
                RestaurantRole.Staff => Roles.RestaurantStaff,
                _ => role.ToString()
            };
            TestAuthenticationService.AddPermissionClaim(roleConstant, restaurantId.ToString());
        }
        
        return userId;
    }

    /// <summary>
    /// Sets up restaurant authorization test environment with required roles and test data.
    /// </summary>
    public static async Task SetupRestaurantAuthorizationTestsAsync()
    {
        await TestUserManager.EnsureRolesExistAsync(TestConfiguration.TestRoles.AllRoles);
    }
}
