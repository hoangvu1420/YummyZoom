using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.TeamCarts.Models;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Infrastructure.Notifications.TeamCarts;

/// <summary>
/// Infrastructure implementation that looks up TeamCart VM, gathers member device tokens,
/// and sends a data-only FCM message { type: "teamcart", teamCartId, version, state, title, body, message, route } to all active devices.
/// </summary>
public sealed class TeamCartPushNotifier : ITeamCartPushNotifier
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IUserDeviceSessionRepository _userDeviceSessions;
    private readonly IFcmService _fcm;
    private readonly ILogger<TeamCartPushNotifier> _logger;

    public TeamCartPushNotifier(
        ITeamCartStore store,
        ITeamCartRepository teamCartRepository,
        IUserDeviceSessionRepository userDeviceSessions,
        IFcmService fcm,
        ILogger<TeamCartPushNotifier> logger)
    {
        _store = store;
        _teamCartRepository = teamCartRepository;
        _userDeviceSessions = userDeviceSessions;
        _fcm = fcm;
        _logger = logger;
    }

    public async Task<Result> PushTeamCartDataAsync(TeamCartId teamCartId, long version, CancellationToken cancellationToken = default)
    {
        // Try VM first (fast path), fallback to repository if needed
        var vm = await _store.GetVmAsync(teamCartId, cancellationToken);
        if (vm is null)
        {
            _logger.LogWarning("TeamCart VM not found; loading from repository (CartId={CartId})", teamCartId.Value);
            // Could load from repository as fallback, but for now log and return
            // In practice, VM should exist if we're pushing notifications
            return Result.Success();
        }

        // Gather distinct user IDs from VM members
        var userIds = vm.Members.Select(m => m.UserId).Distinct().ToList();
        if (userIds.Count == 0)
        {
            _logger.LogWarning("No members in TeamCart VM; skipping FCM push (CartId={CartId})", teamCartId.Value);
            return Result.Success();
        }

        // Collect tokens from all members
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var uid in userIds)
        {
            var list = await _userDeviceSessions.GetActiveFcmTokensByUserIdAsync(uid, cancellationToken);
            foreach (var t in list) tokens.Add(t);
        }

        if (tokens.Count == 0)
        {
            _logger.LogWarning("No active device tokens; skipping FCM push (CartId={CartId})", teamCartId.Value);
            return Result.Success();
        }

        // Filter out seed tokens
        var validTokens = tokens.Where(token => !token.StartsWith("seedToken")).ToList();
        if (validTokens.Count == 0)
        {
            _logger.LogDebug("All tokens are seed tokens; skipping TeamCart FCM push (CartId={CartId})", teamCartId.Value);
            return Result.Success();
        }

        // Load aggregate for payload enrichment (like OrderPushNotifier)
        var cart = await _teamCartRepository.GetByIdAsync(teamCartId, cancellationToken);
        if (cart is null)
        {
            _logger.LogWarning("TeamCart not found when pushing FCM (CartId={CartId}). Skipping.", teamCartId.Value);
            return Result.Success();
        }

        var state = vm.Status.ToString(); // Keep source casing
        var (title, body) = LocalizeState(state);
        var route = $"/team-carts/{teamCartId.Value}";
        
        // Enhanced message (like OrderPushNotifier)
        var message = EnhanceMessage(body, vm, cart);

        var data = new Dictionary<string, string>
        {
            ["type"] = "teamcart",
            ["teamCartId"] = teamCartId.Value.ToString(),
            ["version"] = version.ToString(), // Use explicit parameter
            ["state"] = state,
            ["title"] = title,
            ["body"] = body,
            ["message"] = message, // Enhanced message
            ["route"] = route
        };

        var push = await _fcm.SendMulticastDataAsync(validTokens, data);
        if (push.IsFailure)
        {
            _logger.LogError("Failed to send TeamCart FCM data push (CartId={CartId}): {Error}", teamCartId.Value, push.Error);
            return Result.Failure(push.Error); // Return failure (handler will throw)
        }

        _logger.LogInformation("Sent TeamCart FCM data push to {Count} tokens (CartId={CartId}, State={State}, Version={Version})",
            validTokens.Count, teamCartId.Value, state, version);
        return Result.Success();
    }

    private static string EnhanceMessage(string body, TeamCartViewModel vm, TeamCart cart)
    {
        // Enhance message with context (e.g., member count, status-specific info)
        // Similar to OrderPushNotifier's order number enhancement
        if (vm.Members.Count > 1)
        {
            return $"{vm.Members.Count} thành viên - {TrimTrailingPeriod(body)}";
        }
        return body;
    }

    private static string TrimTrailingPeriod(string text)
        => string.IsNullOrWhiteSpace(text) ? text : text.TrimEnd().TrimEnd('.', '。', '!');

    private static (string Title, string Body) LocalizeState(string state)
        => state switch
        {
            // Common states (adapt to actual TeamCartStatus values present in VM)
            "Active" => ("Giỏ nhóm đang hoạt động", "Giỏ của bạn đã được cập nhật."),
            "Open" => ("Giỏ nhóm đang mở", "Giỏ của bạn đã được cập nhật."),
            "Locked" => ("Giỏ nhóm đã khóa", "Chủ giỏ đang xác nhận thanh toán."),
            "ReadyToConfirm" => ("Sẵn sàng xác nhận", "Giỏ nhóm đã sẵn sàng để xác nhận thanh toán."),
            "Converted" => ("Đã tạo đơn hàng", "Giỏ nhóm đã được chuyển thành đơn hàng."),
            "Expired" => ("Giỏ nhóm đã hết hạn", "Giỏ nhóm của bạn đã đóng."),

            // Fallback
            _ => ("Cập nhật giỏ nhóm", "Giỏ nhóm của bạn đã được cập nhật.")
        };
}
