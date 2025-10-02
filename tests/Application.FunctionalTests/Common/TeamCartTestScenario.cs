using YummyZoom.Application.FunctionalTests.UserManagement;
using YummyZoom.SharedKernel.Constants;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Common;

/// <summary>
/// Represents a complete team cart test scenario with all members and their roles.
/// Provides clean methods to switch between different member contexts during testing.
/// </summary>
public class TeamCartTestScenario
{
    public Guid TeamCartId { get; private set; }
    public string ShareToken { get; private set; }
    public Guid HostUserId { get; private set; }
    public string HostName { get; private set; }
    public Dictionary<string, Guid> GuestUserIds { get; private set; }

    public TeamCartTestScenario(
        Guid teamCartId,
        string shareToken,
        Guid hostUserId,
        string hostName,
        Dictionary<string, Guid> guestUserIds)
    {
        TeamCartId = teamCartId;
        ShareToken = shareToken;
        HostUserId = hostUserId;
        HostName = hostName;
        GuestUserIds = guestUserIds ?? new Dictionary<string, Guid>();
    }

    /// <summary>
    /// Switches to the host user context with proper TeamCartHost permissions.
    /// </summary>
    public Task ActAsHost()
    {
        SetUserId(HostUserId);
        TestAuthenticationService.AddPermissionClaim(Roles.TeamCartHost, TeamCartId.ToString());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Switches to a guest user context with proper TeamCartMember permissions.
    /// </summary>
    /// <param name="guestName">The name of the guest to switch to</param>
    public Task ActAsGuest(string guestName)
    {
        if (!GuestUserIds.TryGetValue(guestName, out var guestUserId))
        {
            throw new ArgumentException($"Guest '{guestName}' not found in scenario. Available guests: {string.Join(", ", GuestUserIds.Keys)}");
        }

        SetUserId(guestUserId);
        TestAuthenticationService.AddPermissionClaim(Roles.TeamCartMember, TeamCartId.ToString());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Switches to a non-member user context for negative testing scenarios.
    /// </summary>
    public async Task ActAsNonMember()
    {
        var nonMemberUserId = await CreateUserAsync("nonmember@example.com", "Password123!");
        SetUserId(nonMemberUserId);
        // Deliberately don't add any TeamCart permission claims
    }

    /// <summary>
    /// Gets the user ID for a specific guest by name.
    /// </summary>
    public Guid GetGuestUserId(string guestName)
    {
        if (!GuestUserIds.TryGetValue(guestName, out var guestUserId))
        {
            throw new ArgumentException($"Guest '{guestName}' not found in scenario. Available guests: {string.Join(", ", GuestUserIds.Keys)}");
        }
        return guestUserId;
    }

    /// <summary>
    /// Gets all member user IDs (host + guests).
    /// </summary>
    public IEnumerable<Guid> GetAllMemberUserIds()
    {
        yield return HostUserId;
        foreach (var guestUserId in GuestUserIds.Values)
        {
            yield return guestUserId;
        }
    }
}
