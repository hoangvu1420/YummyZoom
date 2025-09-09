using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.SharedKernel.Constants;
using YummyZoom.Application.FunctionalTests.UserManagement;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.FunctionalTests.Common;

namespace YummyZoom.Application.FunctionalTests.Authorization;

/// <summary>
/// Helper for setting up TeamCart authorization in functional tests.
/// Provides methods to run tests as users with specific TeamCart roles and permissions.
/// </summary>
public static class TeamCartRoleTestHelper
{
    /// <summary>
    /// Runs the test as a TeamCart host for the specified team cart.
    /// </summary>
    /// <param name="teamCartId">The ID of the team cart</param>
    /// <param name="email">Optional email for the test user</param>
    /// <param name="password">Optional password for the test user</param>
    /// <returns>The user ID of the created test user</returns>
    public static async Task<Guid> RunAsTeamCartHostAsync(
        Guid teamCartId,
        string? email = null,
        string? password = null)
    {
        var userId = await CreateUserWithTeamCartRoleAsync(
            teamCartId,
            MemberRole.Host,
            email,
            password);

        return userId;
    }

    /// <summary>
    /// Runs the test as a TeamCart member for the specified team cart.
    /// </summary>
    /// <param name="teamCartId">The ID of the team cart</param>
    /// <param name="email">Optional email for the test user</param>
    /// <param name="password">Optional password for the test user</param>
    /// <returns>The user ID of the created test user</returns>
    public static async Task<Guid> RunAsTeamCartMemberAsync(
        Guid teamCartId,
        string? email = null,
        string? password = null)
    {
        var userId = await CreateUserWithTeamCartRoleAsync(
            teamCartId,
            MemberRole.Guest, // Domain uses Guest for members
            email,
            password);

        // Add TeamCartMember permission claim
        TestAuthenticationService.AddPermissionClaim(Roles.TeamCartMember, teamCartId.ToString());

        return userId;
    }

    /// <summary>
    /// Runs the test as a TeamCart guest for the specified team cart.
    /// </summary>
    /// <param name="teamCartId">The ID of the team cart</param>
    /// <param name="email">Optional email for the test user</param>
    /// <param name="password">Optional password for the test user</param>
    /// <returns>The user ID of the created test user</returns>
    public static async Task<Guid> RunAsTeamCartGuestAsync(
        Guid teamCartId,
        string? email = null,
        string? password = null)
    {
        var userId = await CreateUserWithTeamCartRoleAsync(
            teamCartId,
            MemberRole.Guest,
            email,
            password);

        // Add TeamCartGuest permission claim
        TestAuthenticationService.AddPermissionClaim(Roles.TeamCartGuest, teamCartId.ToString());

        return userId;
    }

    /// <summary>
    /// Runs the test as a user with multiple TeamCart roles across different team carts.
    /// </summary>
    /// <param name="teamCartRoles">Dictionary of team cart ID to member role mappings</param>
    /// <param name="email">Optional email for the test user</param>
    /// <param name="password">Optional password for the test user</param>
    /// <returns>The user ID of the created test user</returns>
    public static async Task<Guid> RunAsUserWithMultipleTeamCartRolesAsync(
        Dictionary<Guid, MemberRole> teamCartRoles,
        string? email = null,
        string? password = null)
    {
        if (!teamCartRoles.Any())
        {
            throw new ArgumentException("At least one team cart role must be specified", nameof(teamCartRoles));
        }

        // Use default values if not provided
        email ??= $"teamcart-user-{Guid.NewGuid()}@example.com";
        password ??= TestConfiguration.DefaultUsers.CommonTestPassword;

        // Create the user
        var userId = await TestUserManager.RunAsUserAsync(email, password, Array.Empty<string>());

        // Switch to admin to create team cart memberships if needed
        await TestUserManager.RunAsUserAsync(
            TestConfiguration.DefaultUsers.Administrator.Email,
            TestConfiguration.DefaultUsers.Administrator.Password,
            new[] { Roles.Administrator });

        // TODO: Create actual team cart memberships in the database
        // This would require creating TeamCartMember entities for each role
        // For now, we'll just add permission claims

        // Switch back to the target user and add all permission claims
        TestUserManager.SetCurrentUserId(userId);

        // Add all permission claims
        foreach (var (teamCartId, role) in teamCartRoles)
        {
            var roleConstant = role switch
            {
                MemberRole.Host => Roles.TeamCartHost,
                MemberRole.Guest => Roles.TeamCartMember, // Default guests to members
                _ => throw new ArgumentException($"Unknown member role: {role}")
            };
            TestAuthenticationService.AddPermissionClaim(roleConstant, teamCartId.ToString());
        }

        return userId;
    }

    /// <summary>
    /// Switches to an existing team cart host user with proper permissions.
    /// Use this when you already have a user who is a member of the team cart.
    /// </summary>
    public static Task RunAsExistingTeamCartHostAsync(Guid userId, Guid teamCartId)
    {
        return userId.SwitchToTeamCartMember(teamCartId, MemberRole.Host);
    }

    /// <summary>
    /// Switches to an existing team cart member user with proper permissions.
    /// Use this when you already have a user who is a member of the team cart.
    /// </summary>
    public static Task RunAsExistingTeamCartMemberAsync(Guid userId, Guid teamCartId)
    {
        return userId.SwitchToTeamCartMember(teamCartId, MemberRole.Guest);
    }

    /// <summary>
    /// Sets up a complete team cart scenario with the specified members.
    /// </summary>
    public static async Task<TeamCartTestScenario> SetupTeamCartScenarioAsync(
        Guid restaurantId, 
        string hostName, 
        params string[] guestNames)
    {
        var builder = TeamCartTestBuilder.Create(restaurantId).WithHost(hostName);
        
        foreach (var guestName in guestNames)
        {
            builder.WithGuest(guestName);
        }
        
        return await builder.BuildAsync();
    }

    /// <summary>
    /// Sets up TeamCart authorization test environment with required roles.
    /// </summary>
    public static async Task SetupTeamCartAuthorizationTestsAsync()
    {
        var teamCartRoles = new[]
        {
            Roles.TeamCartHost,
            Roles.TeamCartMember,
            Roles.TeamCartGuest
        };

        await TestUserManager.EnsureRolesExistAsync(teamCartRoles);
    }

    /// <summary>
    /// Creates a user with a specific TeamCart role and permission claims.
    /// </summary>
    private static async Task<Guid> CreateUserWithTeamCartRoleAsync(
        Guid teamCartId,
        MemberRole memberRole,
        string? email = null,
        string? password = null)
    {
        // Use default values if not provided
        email ??= $"teamcart-user-{Guid.NewGuid()}@example.com";
        password ??= TestConfiguration.DefaultUsers.CommonTestPassword;

        // Create the user
        var userId = await TestUserManager.RunAsUserAsync(email, password, Array.Empty<string>());

        // Switch to admin to create team cart membership if needed
        await TestUserManager.RunAsUserAsync(
            TestConfiguration.DefaultUsers.Administrator.Email,
            TestConfiguration.DefaultUsers.Administrator.Password,
            new[] { Roles.Administrator });

        // TODO: Create actual TeamCartMember entity in the database
        // This would involve calling the domain service to add the user to the team cart
        // For now, we'll just add permission claims

        // Switch back to the target user and add permission claims
        TestUserManager.SetCurrentUserId(userId);

        // Add permission claim based on member role
        var roleConstant = memberRole switch
        {
            MemberRole.Host => Roles.TeamCartHost,
            MemberRole.Guest => Roles.TeamCartMember, // Default guests to members for authorization
            _ => throw new ArgumentException($"Unknown member role: {memberRole}")
        };

        TestAuthenticationService.AddPermissionClaim(roleConstant, teamCartId.ToString());

        return userId;
    }
}
