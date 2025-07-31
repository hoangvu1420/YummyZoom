using YummyZoom.Application.FunctionalTests.UserManagement;
using YummyZoom.SharedKernel.Constants;
using YummyZoom.Application.FunctionalTests.Infrastructure;

namespace YummyZoom.Application.FunctionalTests.Authorization;

/// <summary>
/// Helper class for common authorization test setup and configuration.
/// </summary>
public static class AuthorizationTestSetup
{
    /// <summary>
    /// Sets up authorization test environment with all required roles.
    /// This method ensures all necessary roles exist in the system for authorization testing.
    /// </summary>
    public static async Task SetupForAuthorizationTestsAsync()
    {
        await TestUserManager.EnsureRolesExistAsync(TestConfiguration.TestRoles.AllRoles);
    }

    /// <summary>
    /// Sets up basic user roles required for most authorization tests.
    /// </summary>
    public static async Task SetupBasicRolesAsync()
    {
        await TestUserManager.EnsureRolesExistAsync(TestConfiguration.TestRoles.BasicRoles);
    }

    /// <summary>
    /// Sets up restaurant-specific roles for restaurant authorization tests.
    /// </summary>
    public static async Task SetupRestaurantRolesAsync()
    {
        await TestUserManager.EnsureRolesExistAsync(TestConfiguration.TestRoles.RestaurantRoles);
    }

    /// <summary>
    /// Sets up a complete authorization test environment including all roles and basic test data.
    /// This is a comprehensive setup method for complex authorization scenarios.
    /// </summary>
    public static async Task SetupCompleteAuthorizationEnvironmentAsync()
    {
        // Ensure all roles exist
        await SetupForAuthorizationTestsAsync();
        
        // Additional setup can be added here as needed
        // For example: creating test restaurants, test users with specific roles, etc.
    }

    /// <summary>
    /// Validates that all required roles exist in the system.
    /// Useful for debugging authorization test failures.
    /// </summary>
    public static async Task ValidateRequiredRolesExistAsync(params string[] requiredRoles)
    {
        await TestUserManager.EnsureRolesExistAsync(requiredRoles);
    }
}
