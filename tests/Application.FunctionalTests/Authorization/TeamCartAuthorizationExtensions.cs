using YummyZoom.Application.FunctionalTests.UserManagement;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.SharedKernel.Constants;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Authorization;

/// <summary>
/// Extension methods for common TeamCart authorization patterns in functional tests.
/// Provides convenient methods for switching users and managing permissions.
/// </summary>
public static class TeamCartAuthorizationExtensions
{
    /// <summary>
    /// Switches to a team cart member with the appropriate permissions for their role.
    /// </summary>
    public static Task SwitchToTeamCartMember(this Guid userId, Guid teamCartId, MemberRole role)
    {
        SetUserId(userId);

        var permissionRole = role switch
        {
            MemberRole.Host => Roles.TeamCartHost,
            MemberRole.Guest => Roles.TeamCartMember,
            _ => throw new ArgumentException($"Unknown member role: {role}")
        };

        TestAuthenticationService.AddPermissionClaim(permissionRole, teamCartId.ToString());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds TeamCart host permissions to the specified user.
    /// </summary>
    public static Task AddTeamCartHostPermission(this Guid userId, Guid teamCartId)
    {
        return userId.SwitchToTeamCartMember(teamCartId, MemberRole.Host);
    }

    /// <summary>
    /// Adds TeamCart member permissions to the specified user.
    /// </summary>
    public static Task AddTeamCartMemberPermission(this Guid userId, Guid teamCartId)
    {
        return userId.SwitchToTeamCartMember(teamCartId, MemberRole.Guest);
    }

    /// <summary>
    /// Creates a new user and joins them to the team cart as a guest.
    /// Returns the user ID of the created guest.
    /// </summary>
    public static async Task<Guid> CreateAndJoinAsGuestAsync(this Guid teamCartId, string shareToken, string guestName, string? email = null)
    {
        email ??= $"{guestName.Replace(" ", "").ToLower()}@example.com";

        var guestUserId = await CreateUserAsync(email, "Password123!");
        SetUserId(guestUserId);

        var joinCommand = new YummyZoom.Application.TeamCarts.Commands.JoinTeamCart.JoinTeamCartCommand(
            teamCartId, shareToken, guestName);

        var result = await SendAsync(joinCommand);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to join team cart as guest '{guestName}': {result.Error.Description}");
        }

        return guestUserId;
    }
}
