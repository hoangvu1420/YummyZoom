using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Infrastructure.Notifications.TeamCarts;

/// <summary>
/// Infrastructure implementation that looks up TeamCart VM, gathers member device tokens,
/// and sends a data-only FCM message { teamCartId, version } to all active devices.
/// </summary>
public sealed class TeamCartPushNotifier : ITeamCartPushNotifier
{
    private readonly ITeamCartStore _store;
    private readonly IUserDeviceSessionRepository _userDeviceSessions;
    private readonly IFcmService _fcm;
    private readonly ILogger<TeamCartPushNotifier> _logger;

    public TeamCartPushNotifier(
        ITeamCartStore store,
        IUserDeviceSessionRepository userDeviceSessions,
        IFcmService fcm,
        ILogger<TeamCartPushNotifier> logger)
    {
        _store = store;
        _userDeviceSessions = userDeviceSessions;
        _fcm = fcm;
        _logger = logger;
    }

    public async Task<Result> PushTeamCartDataAsync(TeamCartId teamCartId, CancellationToken cancellationToken = default)
    {
        var vm = await _store.GetVmAsync(teamCartId, cancellationToken);
        if (vm is null)
        {
            _logger.LogDebug("TeamCart VM not found; skipping FCM push (CartId={CartId})", teamCartId.Value);
            return Result.Success();
        }

        // Gather distinct user IDs from VM members
        var userIds = vm.Members.Select(m => m.UserId).Distinct().ToList();
        if (userIds.Count == 0)
        {
            _logger.LogDebug("No members in TeamCart VM; skipping FCM push (CartId={CartId})", teamCartId.Value);
            return Result.Success();
        }

        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var uid in userIds)
        {
            var list = await _userDeviceSessions.GetActiveFcmTokensByUserIdAsync(uid, cancellationToken);
            foreach (var t in list) tokens.Add(t);
        }

        if (tokens.Count == 0)
        {
            _logger.LogDebug("No active device tokens; skipping FCM push (CartId={CartId})", teamCartId.Value);
            return Result.Success();
        }

        var data = new Dictionary<string, string>
        {
            ["type"] = "teamcart",
            ["teamCartId"] = teamCartId.Value.ToString(),
            ["version"] = vm.Version.ToString()
        };

        var push = await _fcm.SendMulticastDataAsync(tokens, data);
        if (push.IsFailure)
        {
            _logger.LogError("Failed to send TeamCart FCM data push (CartId={CartId}): {Error}", teamCartId.Value, push.Error);
            return Result.Failure(push.Error);
        }

        _logger.LogInformation("Sent TeamCart FCM data push to {Count} tokens (CartId={CartId}, Version={Version})",
            tokens.Count, teamCartId.Value, vm.Version);
        return Result.Success();
    }
}
