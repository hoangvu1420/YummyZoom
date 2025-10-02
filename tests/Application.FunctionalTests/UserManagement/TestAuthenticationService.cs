using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;

namespace YummyZoom.Application.FunctionalTests.UserManagement;

/// <summary>
/// Manages authentication state and claims handling for functional tests.
/// </summary>
public static class TestAuthenticationService
{
    /// <summary>
    /// Refreshes user claims from the current database state.
    /// </summary>
    public static async Task RefreshUserClaimsAsync()
    {
        var testUserService = CustomWebApplicationFactory.GetTestUserService();
        var factory = TestInfrastructure.GetFactory();

        if (factory?.Services != null)
        {
            await testUserService.RefreshClaimsFromDatabase(factory.Services);
        }
    }

    /// <summary>
    /// Adds a permission claim for the specified role and resource.
    /// </summary>
    public static void AddPermissionClaim(string role, string resourceId)
    {
        var testUserService = CustomWebApplicationFactory.GetTestUserService();
        testUserService.AddPermissionClaim(role, resourceId);
    }

    /// <summary>
    /// Removes a permission claim for the specified role and resource.
    /// </summary>
    public static void RemovePermissionClaim(string role, string resourceId)
    {
        var testUserService = CustomWebApplicationFactory.GetTestUserService();
        testUserService.RemovePermissionClaim(role, resourceId);
    }

    /// <summary>
    /// Adds administrator claims to the current user.
    /// </summary>
    public static void AddAdminClaim()
    {
        var testUserService = CustomWebApplicationFactory.GetTestUserService();
        testUserService.AddAdminClaim();
    }

    /// <summary>
    /// Clears all authentication state and claims.
    /// </summary>
    public static void ClearAuthenticationState()
    {
        var testUserService = CustomWebApplicationFactory.GetTestUserService();
        testUserService.SetUserId(null);
        // Additional cleanup can be added here as needed
    }

    /// <summary>
    /// Gets the test user service instance for advanced operations.
    /// </summary>
    public static dynamic GetTestUserService()
    {
        return CustomWebApplicationFactory.GetTestUserService();
    }
}
