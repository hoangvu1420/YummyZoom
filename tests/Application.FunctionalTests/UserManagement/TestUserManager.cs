using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Infrastructure.Identity;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.FunctionalTests.UserManagement;

/// <summary>
/// Manages user creation, authentication, and basic user operations for functional tests.
/// </summary>
public static class TestUserManager
{
    private static Guid? _currentUserId;

    /// <summary>
    /// Gets the current user ID.
    /// </summary>
    public static Guid? GetCurrentUserId()
    {
        return _currentUserId;
    }

    /// <summary>
    /// Sets the current user ID and updates the test user service context.
    /// </summary>
    public static void SetCurrentUserId(Guid? userId)
    {
        _currentUserId = userId;

        // Update the TestUserService with the new user context
        var testUserService = CustomWebApplicationFactory.GetTestUserService();
        testUserService.SetUserId(userId);
    }

    /// <summary>
    /// Creates a user with the specified email, password, and roles.
    /// If the user already exists, it ensures they have the specified roles.
    /// </summary>
    public static async Task<Guid> CreateUserAsync(string email, string password, params string[] roles)
    {
        await EnsureRolesExistAsync(roles);

        using var scope = TestInfrastructure.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser { UserName = email, Email = email };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(Environment.NewLine, result.ToApplicationResult().Errors);
                throw new Exception($"Unable to create {email}.{Environment.NewLine}{errors}");
            }
        }

        if (roles.Any())
        {
            var userRoles = await userManager.GetRolesAsync(user);
            var missingRoles = roles.Except(userRoles).ToArray();
            if (missingRoles.Any())
            {
                await userManager.AddToRolesAsync(user, missingRoles);
            }
        }

        return user.Id;
    }

    /// <summary>
    /// Runs tests as the specified user, creating the user if necessary.
    /// </summary>
    public static async Task<Guid> RunAsUserAsync(string email, string password, params string[] roles)
    {
        var userId = await CreateUserAsync(email, password, roles);

        SetCurrentUserId(userId);

        var testUserService = CustomWebApplicationFactory.GetTestUserService();

        // Add administrator claims if user has Administrator role
        if (roles.Contains(Roles.Administrator))
        {
            testUserService.AddAdminClaim();
        }

        // Add role claims for provided roles to satisfy role-based checks in policies
        if (roles is { Length: > 0 })
        {
            foreach (var role in roles)
            {
                if (!string.IsNullOrWhiteSpace(role))
                {
                    testUserService.AddRoleClaim(role);
                }
            }
        }

        return userId;
    }

    /// <summary>
    /// Runs tests as the default test user.
    /// </summary>
    public static async Task<Guid> RunAsDefaultUserAsync()
    {
        // Default user models a fully onboarded customer; grant baseline 'User' role for CompletedSignup policy.
        return await RunAsUserAsync(TestConfiguration.DefaultUsers.TestUser.Email, TestConfiguration.DefaultUsers.TestUser.Password, new[] { Roles.User });
    }

    /// <summary>
    /// Runs tests as an administrator user.
    /// </summary>
    public static async Task<Guid> RunAsAdministratorAsync()
    {
        return await RunAsUserAsync(TestConfiguration.DefaultUsers.Administrator.Email, TestConfiguration.DefaultUsers.Administrator.Password, new[] { Roles.Administrator });
    }

    /// <summary>
    /// Ensures the specified roles exist in the system.
    /// </summary>
    public static async Task EnsureRolesExistAsync(params string[]? roleNames)
    {
        if (roleNames == null || roleNames.Length == 0)
        {
            return;
        }

        using var scope = TestInfrastructure.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var roleName in roleNames)
        {
            if (string.IsNullOrWhiteSpace(roleName)) continue;

            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"Warning: Failed to create role {roleName} during test setup. Errors: {errors}");
                }
            }
        }
    }

    /// <summary>
    /// Sets up roles required for user registration tests.
    /// </summary>
    public static async Task SetupForUserRegistrationTestsAsync()
    {
        await EnsureRolesExistAsync(TestConfiguration.TestRoles.UserRegistrationRoles);
    }

    /// <summary>
    /// Clears the current user context.
    /// </summary>
    public static void ClearUserContext()
    {
        _currentUserId = null;

        var testUserService = CustomWebApplicationFactory.GetTestUserService();
        testUserService.SetUserId(null);
    }
}
