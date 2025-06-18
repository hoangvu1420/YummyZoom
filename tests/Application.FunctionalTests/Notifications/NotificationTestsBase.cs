using YummyZoom.Application.Users.Commands.RegisterDevice;
using YummyZoom.SharedKernel.Constants;
using YummyZoom.Infrastructure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.SharedKernel.Models;

namespace YummyZoom.Application.FunctionalTests.Notifications;

using static Testing;

/// <summary>
/// Base class for notification tests that provides shared helper methods
/// and ensures proper user context management.
/// </summary>
public abstract class NotificationTestsBase : BaseTestFixture
{
    #region Context-Preserving Helper Methods

    /// <summary>
    /// Sets up a user with a device while preserving the current user context.
    /// </summary>
    protected static async Task<Guid> SetupUserWithDeviceAsync(string email, string fcmToken, string platform = "iOS")
    {
        // Remember the current user context
        var currentUser = GetCurrentUserId();
        
        // Create user and register device
        var userId = await RunAsUserAsync(email, "Password123!", new[] { Roles.User });
        
        var registerCommand = new RegisterDeviceCommand(fcmToken, platform);
        var result = await SendAsync(registerCommand);
        result.ShouldBeSuccessful();
        
        // Restore the previous user context if one existed
        if (currentUser.HasValue)
        {
            await RestoreUserContextAsync(currentUser.Value);
        }
        
        return userId;
    }

    /// <summary>
    /// Sets up a user with multiple devices while preserving the current user context.
    /// </summary>
    protected static async Task<Guid> SetupUserWithMultipleDevicesAsync(string email, string[] fcmTokens, string platform = "iOS")
    {
        // Remember the current user context
        var currentUser = GetCurrentUserId();
        
        // Create user and register multiple devices
        var userId = await RunAsUserAsync(email, "Password123!", new[] { Roles.User });
        
        foreach (var token in fcmTokens)
        {
            var registerCommand = new RegisterDeviceCommand(token, platform);
            var result = await SendAsync(registerCommand);
            result.ShouldBeSuccessful();
        }
        
        // Restore the previous user context if one existed
        if (currentUser.HasValue)
        {
            await RestoreUserContextAsync(currentUser.Value);
        }
        
        return userId;
    }

    /// <summary>
    /// Sets up a user with an inactive device while preserving the current user context.
    /// </summary>
    protected static async Task<Guid> SetupUserWithInactiveDeviceAsync(string email, string fcmToken, string platform = "iOS")
    {
        var userId = await SetupUserWithDeviceAsync(email, fcmToken, platform);

        // Mark the session as inactive
        using var scope = CreateScope();
        var userDeviceSessionRepository = scope.ServiceProvider.GetRequiredService<IUserDeviceSessionRepository>();

        var session = await userDeviceSessionRepository.GetActiveSessionByTokenAsync(fcmToken);
        if (session != null)
        {
            session.IsActive = false;
            session.LoggedOutAt = DateTime.UtcNow;
            // Assuming SaveChangesAsync is handled by the repository or UoW
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        return userId;
    }

    /// <summary>
    /// Sets up multiple users with devices while preserving the current user context.
    /// </summary>
    protected static async Task SetupMultipleUsersWithDevicesAsync(params (string email, string token)[] users)
    {
        foreach (var (email, token) in users)
        {
            await SetupUserWithDeviceAsync(email, token);
        }
    }

    /// <summary>
    /// Sets up users with mixed device states (some active, some inactive) while preserving context.
    /// </summary>
    protected static async Task SetupUsersWithMixedDeviceStatesAsync()
    {
        // Setup some active devices
        await SetupUserWithDeviceAsync("active1@test.com", "active-token-1");
        await SetupUserWithDeviceAsync("active2@test.com", "active-token-2");
        
        // Setup inactive device
        await SetupUserWithInactiveDeviceAsync("inactive@test.com", "inactive-token");
    }

    /// <summary>
    /// Sets up users with only inactive devices while preserving context.
    /// </summary>
    protected static async Task SetupUsersWithOnlyInactiveDevicesAsync()
    {
        var users = new[]
        {
            ("inactive1@test.com", "inactive-token-1"),
            ("inactive2@test.com", "inactive-token-2")
        };

        foreach (var (email, token) in users)
        {
            await SetupUserWithInactiveDeviceAsync(email, token);
        }
    }

    /// <summary>
    /// Sets up a large user base for performance testing while preserving context.
    /// </summary>
    protected static async Task SetupLargeUserBaseAsync(int userCount = 8)
    {
        for (int i = 1; i <= userCount; i++)
        {
            await SetupUserWithDeviceAsync($"largeuser{i}@test.com", $"large-token-{i}");
        }
    }

    /// <summary>
    /// Sets up users for complete workflow testing while preserving context.
    /// </summary>
    protected static async Task SetupCompleteWorkflowUsersAsync()
    {
        // Remember the current user context
        var currentUser = GetCurrentUserId();
        
        // User 1 with multiple devices
        var user1Id = await RunAsUserAsync("workflow1@test.com", "Password123!", new[] { Roles.User });
        await SendAsync(new RegisterDeviceCommand("workflow-token-1a", "iOS", "iPhone-1"));
        await SendAsync(new RegisterDeviceCommand("workflow-token-1b", "Android", "Samsung-1"));
        
        // User 2 with single device
        var user2Id = await RunAsUserAsync("workflow2@test.com", "Password123!", new[] { Roles.User });
        await SendAsync(new RegisterDeviceCommand("workflow-token-2", "iOS", "iPhone-2"));
        
        // User 3 with single device
        var user3Id = await RunAsUserAsync("workflow3@test.com", "Password123!", new[] { Roles.User });
        await SendAsync(new RegisterDeviceCommand("workflow-token-3", "Android", "Pixel-3"));
        
        // Restore the previous user context if one existed
        if (currentUser.HasValue)
        {
            await RestoreUserContextAsync(currentUser.Value);
        }
    }

    #endregion

    #region Query Helper Methods

    /// <summary>
    /// Counts all active sessions in the system.
    /// </summary>
    protected static async Task<int> CountAllActiveSessionsAsync()
    {
        using var scope = CreateScope();
        var userDeviceSessionRepository = scope.ServiceProvider.GetRequiredService<IUserDeviceSessionRepository>();
        var activeTokens = await userDeviceSessionRepository.GetAllActiveFcmTokensAsync();
        return activeTokens.Count;
    }

    /// <summary>
    /// Counts active sessions for a specific user.
    /// </summary>
    protected static async Task<int> CountActiveSessionsForUserAsync(Guid userId)
    {
        using var scope = CreateScope();
        var userDeviceSessionRepository = scope.ServiceProvider.GetRequiredService<IUserDeviceSessionRepository>();
        var activeTokens = await userDeviceSessionRepository.GetActiveFcmTokensByUserIdAsync(userId);
        return activeTokens.Count;
    }

    /// <summary>
    /// Finds an active session by its FCM token.
    /// </summary>
    protected static async Task<UserDeviceSession?> FindActiveSessionByTokenAsync(string fcmToken)
    {
        using var scope = CreateScope();
        var userDeviceSessionRepository = scope.ServiceProvider.GetRequiredService<IUserDeviceSessionRepository>();
        return await userDeviceSessionRepository.GetActiveSessionByTokenAsync(fcmToken);
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Gets the current user ID from the mocked IUser service.
    /// </summary>
    private static Guid? GetCurrentUserId()
    {
        using var scope = CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUser>();
        
        // IUser.Id is a string, so we need to parse it to Guid
        if (string.IsNullOrEmpty(userService.Id))
            return null;
            
        return Guid.TryParse(userService.Id, out var userId) ? userId : null;
    }

    /// <summary>
    /// Restores the user context by setting up the specified user as current.
    /// </summary>
    private static async Task RestoreUserContextAsync(Guid userId)
    {
        // Find the user by ID in the Identity Users table (AspNetUsers)
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId.ToString());
        
        if (user != null)
        {
            // Get user roles to restore context properly
            var roles = await userManager.GetRolesAsync(user);
            
            // Restore the user context
            await RunAsUserAsync(user.Email!, "Password123!", roles.ToArray());
        }
    }

    #endregion
}
