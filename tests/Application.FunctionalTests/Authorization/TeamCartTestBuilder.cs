using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Application.TeamCarts.Commands.JoinTeamCart;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Authorization;

/// <summary>
/// Fluent builder for creating TeamCart test scenarios with proper member setup.
/// Handles both user creation and team cart membership in a clean, reusable way.
/// </summary>
public class TeamCartTestBuilder
{
    private readonly Guid _restaurantId;
    private string? _hostName;
    private string? _hostEmail;
    private readonly List<(string name, string? email)> _guests = new();

    private TeamCartTestBuilder(Guid restaurantId)
    {
        _restaurantId = restaurantId;
    }

    /// <summary>
    /// Creates a new TeamCart test builder for the specified restaurant.
    /// </summary>
    public static TeamCartTestBuilder Create(Guid restaurantId)
    {
        return new TeamCartTestBuilder(restaurantId);
    }

    /// <summary>
    /// Configures the host user for the team cart.
    /// </summary>
    public TeamCartTestBuilder WithHost(string hostName, string? email = null)
    {
        _hostName = hostName;
        _hostEmail = email;
        return this;
    }

    /// <summary>
    /// Adds a guest user to the team cart.
    /// </summary>
    public TeamCartTestBuilder WithGuest(string guestName, string? email = null)
    {
        _guests.Add((guestName, email));
        return this;
    }

    /// <summary>
    /// Adds multiple guest users to the team cart.
    /// </summary>
    public TeamCartTestBuilder WithMultipleGuests(params string[] guestNames)
    {
        foreach (var guestName in guestNames)
        {
            _guests.Add((guestName, null));
        }
        return this;
    }

    /// <summary>
    /// Builds the complete team cart scenario with all members properly set up.
    /// </summary>
    public async Task<TeamCartTestScenario> BuildAsync()
    {
        // Validate required fields
        if (string.IsNullOrEmpty(_hostName))
        {
            throw new InvalidOperationException("Host name is required. Call WithHost() before BuildAsync().");
        }

        // Step 1: Create host user and team cart
        var hostUserId = await RunAsDefaultUserAsync();
        var createResult = await SendAsync(new CreateTeamCartCommand(_restaurantId, _hostName));

        if (createResult.IsFailure)
        {
            throw new InvalidOperationException($"Failed to create team cart: {createResult.Error.Description}");
        }

        var teamCartId = createResult.Value.TeamCartId;
        var shareToken = createResult.Value.ShareToken;

        // Step 2: Create and add guest users
        var guestUserIds = new Dictionary<string, Guid>();

        foreach (var (guestName, guestEmail) in _guests)
        {
            var email = guestEmail ?? $"{guestName.Replace(" ", "").ToLower()}@example.com";
            var guestUserId = await CreateUserAsync(email, "Password123!");

            // Switch to guest user and join the team cart
            SetUserId(guestUserId);
            var joinResult = await SendAsync(new JoinTeamCartCommand(teamCartId, shareToken, guestName));

            if (joinResult.IsFailure)
            {
                throw new InvalidOperationException($"Failed to join team cart as guest '{guestName}': {joinResult.Error.Description}");
            }

            guestUserIds[guestName] = guestUserId;
        }

        // Step 3: Create and return the scenario
        return new TeamCartTestScenario(
            teamCartId,
            shareToken,
            hostUserId,
            _hostName,
            guestUserIds
        );
    }
}
