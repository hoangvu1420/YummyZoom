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
/// and sends FCM messages (hybrid or data-only) to all active devices.
/// Hybrid: notification { title, body, sound }, data { type, teamCartId, version, click_action, route, actorId, event }
/// Data-only: data { type, teamCartId, version, click_action, route, actorId, event }
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

    public async Task<Result> PushTeamCartDataAsync(
        TeamCartId teamCartId, 
        long version,
        TeamCartNotificationTarget target = TeamCartNotificationTarget.All,
        TeamCartNotificationContext? context = null,
        NotificationDeliveryType deliveryType = NotificationDeliveryType.Hybrid,
        CancellationToken cancellationToken = default)
    {
        // Try VM first (fast path), fallback to repository if needed
        var vm = await _store.GetVmAsync(teamCartId, cancellationToken);
        if (vm is null)
        {
            _logger.LogWarning("TeamCart VM not found; loading from repository (CartId={CartId})", teamCartId.Value);
            return Result.Success();
        }

        // Collect valid tokens
        var validTokens = await CollectValidTokensAsync(vm, target, context?.ActorUserId, cancellationToken);
        if (validTokens.Count == 0)
        {
            return Result.Success();
        }

        // Load aggregate for payload enrichment
        var cart = await _teamCartRepository.GetByIdAsync(teamCartId, cancellationToken);
        if (cart is null)
        {
            _logger.LogWarning("TeamCart not found when pushing FCM (CartId={CartId}). Skipping.", teamCartId.Value);
            return Result.Success();
        }

        // Generate contextual messages (needed for both hybrid and data-only)
        var (title, body) = GenerateContextualMessage(vm, cart, target, context);

        // Create data payload (includes title and body for data-only notifications)
        var data = CreateDataPayload(teamCartId, version, vm, context, title, body);

        // Send based on delivery type
        if (deliveryType == NotificationDeliveryType.DataOnly)
        {
            var push = await _fcm.SendMulticastDataAsync(validTokens, data);
            if (push.IsFailure)
            {
                _logger.LogError("Failed to send TeamCart FCM data-only push (CartId={CartId}): {Error}", teamCartId.Value, push.Error);
                return Result.Failure(push.Error);
            }

            _logger.LogInformation("Sent TeamCart FCM data-only push to {Count} tokens (CartId={CartId}, Target={Target}, EventType={EventType}, Version={Version})",
                validTokens.Count, teamCartId.Value, target, context?.EventType ?? "Unknown", version);
            return Result.Success();
        }
        else
        {
            var push = await _fcm.SendMulticastNotificationAsync(validTokens, title, body, data);
            if (push.IsFailure)
            {
                _logger.LogError("Failed to send TeamCart FCM hybrid push (CartId={CartId}): {Error}", teamCartId.Value, push.Error);
                return Result.Failure(push.Error);
            }

            _logger.LogInformation("Sent TeamCart FCM hybrid push to {Count} tokens (CartId={CartId}, Target={Target}, EventType={EventType}, Version={Version})",
                validTokens.Count, teamCartId.Value, target, context?.EventType ?? "Unknown", version);
            return Result.Success();
        }
    }

    private async Task<List<string>> CollectValidTokensAsync(
        TeamCartViewModel vm,
        TeamCartNotificationTarget target,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        // Determine target user IDs based on target type
        var targetUserIds = GetTargetUserIds(vm, target, actorUserId);
        if (targetUserIds.Count == 0)
        {
            _logger.LogDebug("No target users for notification (CartId={CartId}, Target={Target})", vm.CartId, target);
            return [];
        }

        // Collect tokens from target users
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var uid in targetUserIds)
        {
            var list = await _userDeviceSessions.GetActiveFcmTokensByUserIdAsync(uid, cancellationToken);
            foreach (var t in list) tokens.Add(t);
        }

        if (tokens.Count == 0)
        {
            _logger.LogWarning("No active device tokens; skipping FCM push (CartId={CartId})", vm.CartId);
            return [];
        }

        // Filter out seed tokens
        var validTokens = tokens.Where(token => !token.StartsWith("seedToken")).ToList();
        if (validTokens.Count == 0)
        {
            _logger.LogDebug("All tokens are seed tokens; skipping TeamCart FCM push (CartId={CartId})", vm.CartId);
            return [];
        }

        return validTokens;
    }

    private static Dictionary<string, string> CreateDataPayload(
        TeamCartId teamCartId,
        long version,
        TeamCartViewModel vm,
        TeamCartNotificationContext? context,
        string title,
        string body)
    {
        var route = $"/teamcart/{teamCartId.Value}";
        var state = vm.Status.ToString(); // Keep source casing

        return new Dictionary<string, string>
        {
            ["type"] = "teamcart",
            ["teamCartId"] = teamCartId.Value.ToString(),
            ["version"] = version.ToString(),
            ["state"] = state,
            ["click_action"] = "FLUTTER_NOTIFICATION_CLICK",
            ["route"] = route,
            ["actorId"] = context?.ActorUserId?.ToString() ?? "",
            ["event"] = MapEventTypeToEnum(context?.EventType),
            ["title"] = title,
            ["body"] = body
        };
    }

    private static string MapEventTypeToEnum(string? eventType)
    {
        return eventType switch
        {
            "MemberJoined" => "member_joined",
            "ItemAdded" => "item_added",
            "ItemRemoved" => "item_removed",
            "ItemQuantityUpdated" => "item_quantity_updated",
            "TeamCartLockedForPayment" => "cart_locked",
            "TipApplied" => "tip_applied",
            "CouponApplied" => "coupon_applied",
            "CouponRemoved" => "coupon_removed",
            "MemberCommittedToPayment" => "payment_committed",
            "OnlinePaymentSucceeded" => "payment_succeeded",
            "OnlinePaymentFailed" => "payment_failed",
            "TeamCartReadyForConfirmation" => "ready_for_confirmation",
            "TeamCartConverted" => "cart_converted",
            "TeamCartExpired" => "cart_expired",
            "MemberReadyStatusChanged" => "member_ready_changed",
            _ => "state_changed"
        };
    }

    private static List<Guid> GetTargetUserIds(
        TeamCartViewModel vm, 
        TeamCartNotificationTarget target, 
        Guid? actorUserId)
    {
        var hostUserId = vm.Members.FirstOrDefault(m => m.Role == "Host")?.UserId;
        var allUserIds = vm.Members.Select(m => m.UserId).Distinct().ToList();

        return target switch
        {
            TeamCartNotificationTarget.All => allUserIds,
            TeamCartNotificationTarget.Host => hostUserId.HasValue ? [hostUserId.Value] : [],
            TeamCartNotificationTarget.Members => allUserIds.Where(uid => uid != hostUserId).ToList(),
            TeamCartNotificationTarget.SpecificUser => actorUserId.HasValue ? [actorUserId.Value] : [],
            TeamCartNotificationTarget.Others => actorUserId.HasValue 
                ? allUserIds.Where(uid => uid != actorUserId.Value).ToList() 
                : allUserIds,
            _ => allUserIds
        };
    }

    private static (string Title, string Body) GenerateContextualMessage(
        TeamCartViewModel vm,
        TeamCart cart,
        TeamCartNotificationTarget target,
        TeamCartNotificationContext? context)
    {
        var isHostTarget = target == TeamCartNotificationTarget.Host;
        var hostUserId = vm.Members.FirstOrDefault(m => m.Role == "Host")?.UserId;

        // If no context provided, fall back to state-based messages
        if (context is null)
        {
            var state = vm.Status.ToString();
            var (stateTitle, stateBody) = LocalizeState(state);
            return (stateTitle, stateBody);
        }

        // Generate event-specific contextual messages
        var result = context.EventType switch
        {
            "MemberJoined" => GenerateMemberJoinedMessage(vm, context, isHostTarget),
            "ItemAdded" => GenerateItemAddedMessage(vm, context, isHostTarget),
            "ItemRemoved" => GenerateItemRemovedMessage(vm, context, isHostTarget),
            "ItemQuantityUpdated" => GenerateItemQuantityUpdatedMessage(vm, context, isHostTarget),
            "TeamCartLockedForPayment" => GenerateLockedMessage(vm, context, isHostTarget),
            "TipApplied" => GenerateTipAppliedMessage(vm, context, isHostTarget, hostUserId),
            "CouponApplied" => GenerateCouponAppliedMessage(vm, context, isHostTarget, hostUserId),
            "CouponRemoved" => GenerateCouponRemovedMessage(vm, context, isHostTarget, hostUserId),
            "MemberCommittedToPayment" => GeneratePaymentCommittedMessage(vm, context, isHostTarget),
            "OnlinePaymentSucceeded" => GeneratePaymentSucceededMessage(vm, context, isHostTarget, hostUserId),
            "OnlinePaymentFailed" => GeneratePaymentFailedMessage(vm, context, isHostTarget),
            "TeamCartReadyForConfirmation" => GenerateReadyToConfirmMessage(vm, context, isHostTarget),
            "TeamCartConverted" => GenerateConvertedMessage(vm, context, isHostTarget),
            "TeamCartExpired" => GenerateExpiredMessage(vm, context, isHostTarget),
            _ => GenerateDefaultMessage(vm, context)
        };

        return result;
    }

    private static (string Title, string Body) GenerateMemberJoinedMessage(
        TeamCartViewModel vm, TeamCartNotificationContext context, bool isHostTarget)
    {
        var actorName = context.ActorName ?? "Thành viên";
        if (isHostTarget)
        {
            return ("Thành viên mới", $"{actorName} đã tham gia giỏ nhóm của bạn");
        }
        return ("Thành viên mới", $"{actorName} đã tham gia giỏ nhóm");
    }

    private static (string Title, string Body) GenerateItemAddedMessage(
        TeamCartViewModel vm, TeamCartNotificationContext context, bool isHostTarget)
    {
        var actorName = context.ActorName ?? "Ai đó";
        var itemName = context.ItemName ?? "món";
        var quantity = context.Quantity > 1 ? $" (x{context.Quantity})" : "";
        var body = $"{actorName} đã thêm {itemName}{quantity}";
        return ("Món mới", body);
    }

    private static (string Title, string Body) GenerateItemRemovedMessage(
        TeamCartViewModel vm, TeamCartNotificationContext context, bool isHostTarget)
    {
        var actorName = context.ActorName ?? "Ai đó";
        var itemName = context.ItemName ?? "món";
        var body = $"{actorName} đã xóa {itemName}";
        return ("Món đã xóa", body);
    }

    private static (string Title, string Body) GenerateItemQuantityUpdatedMessage(
        TeamCartViewModel vm, TeamCartNotificationContext context, bool isHostTarget)
    {
        var actorName = context.ActorName ?? "Ai đó";
        var itemName = context.ItemName ?? "món";
        var quantity = context.Quantity ?? 1;
        var body = $"{actorName} đã cập nhật số lượng {itemName} thành x{quantity}";
        return ("Cập nhật số lượng", body);
    }

    private static (string Title, string Body) GenerateLockedMessage(
        TeamCartViewModel vm, TeamCartNotificationContext context, bool isHostTarget)
    {
        if (isHostTarget)
        {
            return ("Giỏ nhóm đã khóa", "Bạn đã khóa giỏ nhóm. Vui lòng áp dụng tip/coupon nếu cần.");
        }
        var member = vm.Members.FirstOrDefault(m => m.UserId != vm.Members.First(m => m.Role == "Host").UserId);
        var quotedAmount = member?.QuotedAmount ?? 0m;
        var currency = vm.Currency;
        var amountText = quotedAmount > 0 ? $" {quotedAmount:N0} {currency}" : "";
        return ("Giỏ nhóm đã khóa", $"Giỏ nhóm đã được khóa. Vui lòng thanh toán phần của bạn{amountText}.");
    }

    private static (string Title, string Body) GenerateTipAppliedMessage(
        TeamCartViewModel vm, TeamCartNotificationContext context, bool isHostTarget, Guid? hostUserId)
    {
        var amount = context.Amount ?? 0m;
        var currency = context.Currency ?? vm.Currency;
        var amountText = $"{amount:N0} {currency}";
        
        if (isHostTarget || (context.ActorUserId == hostUserId))
        {
            return ("Tip đã thêm", $"Bạn đã thêm tip {amountText}");
        }
        return ("Tip đã thêm", $"Chủ giỏ đã thêm tip {amountText}. Phần của bạn đã được cập nhật.");
    }

    private static (string Title, string Body) GenerateCouponAppliedMessage(
        TeamCartViewModel vm, TeamCartNotificationContext context, bool isHostTarget, Guid? hostUserId)
    {
        var couponCode = context.AdditionalInfo ?? "mã giảm giá";
        
        if (isHostTarget || (context.ActorUserId == hostUserId))
        {
            return ("Mã giảm giá", $"Bạn đã áp dụng mã giảm giá {couponCode}");
        }
        return ("Mã giảm giá", $"Chủ giỏ đã áp dụng mã giảm giá {couponCode}. Phần của bạn đã được cập nhật.");
    }

    private static (string Title, string Body) GenerateCouponRemovedMessage(
        TeamCartViewModel vm, TeamCartNotificationContext context, bool isHostTarget, Guid? hostUserId)
    {
        if (isHostTarget || (context.ActorUserId == hostUserId))
        {
            return ("Mã giảm giá", "Bạn đã xóa mã giảm giá");
        }
        return ("Mã giảm giá", "Chủ giỏ đã xóa mã giảm giá. Phần của bạn đã được cập nhật.");
    }

    private static (string Title, string Body) GeneratePaymentCommittedMessage(
        TeamCartViewModel vm, TeamCartNotificationContext context, bool isHostTarget)
    {
        var actorName = context.ActorName ?? "Thành viên";
        var amount = context.Amount ?? 0m;
        var currency = context.Currency ?? vm.Currency;
        var method = context.AdditionalInfo == "CODCommitted" ? "COD" : "Online";
        var body = $"{actorName} đã cam kết thanh toán {amount:N0} {currency} bằng {method}";
        return ("Cam kết thanh toán", body);
    }

    private static (string Title, string Body) GeneratePaymentSucceededMessage(
        TeamCartViewModel vm, TeamCartNotificationContext context, bool isHostTarget, Guid? hostUserId)
    {
        var amount = context.Amount ?? 0m;
        var currency = context.Currency ?? vm.Currency;
        var amountText = $"{amount:N0} {currency}";
        
        // Check if this is for the payer (actor is the one who paid)
        var isPayer = context.ActorUserId.HasValue && 
                     vm.Members.Any(m => m.UserId == context.ActorUserId.Value);
        
        if (isPayer && !isHostTarget)
        {
            return ("Thanh toán thành công", $"Bạn đã thanh toán thành công {amountText}");
        }
        
        // Host or others viewing payment
        var actorName = context.ActorName ?? "Thành viên";
        var body = $"{actorName} đã thanh toán thành công {amountText}";
        return ("Thanh toán thành công", body);
    }

    private static (string Title, string Body) GeneratePaymentFailedMessage(
        TeamCartViewModel vm, TeamCartNotificationContext context, bool isHostTarget)
    {
        return ("Thanh toán thất bại", "Thanh toán của bạn thất bại. Vui lòng thử lại.");
    }

    private static (string Title, string Body) GenerateReadyToConfirmMessage(
        TeamCartViewModel vm, TeamCartNotificationContext context, bool isHostTarget)
    {
        if (isHostTarget)
        {
            return ("Sẵn sàng tạo đơn", "Tất cả thành viên đã thanh toán. Bạn có thể tạo đơn hàng ngay!");
        }
        return ("Sẵn sàng tạo đơn", "Tất cả đã thanh toán. Chủ giỏ sẽ tạo đơn.");
    }

    private static (string Title, string Body) GenerateConvertedMessage(
        TeamCartViewModel vm, TeamCartNotificationContext context, bool isHostTarget)
    {
        var orderIdText = context.OrderId.HasValue ? $" #{context.OrderId.Value.ToString()[..8]}" : "";
        var body = $"Giỏ nhóm đã được chuyển thành đơn hàng{orderIdText}";
        return ("Đã tạo đơn hàng", body);
    }

    private static (string Title, string Body) GenerateExpiredMessage(
        TeamCartViewModel vm, TeamCartNotificationContext context, bool isHostTarget)
    {
        return ("Giỏ nhóm đã hết hạn", "Giỏ nhóm đã hết hạn và đã được đóng.");
    }

    private static (string Title, string Body) GenerateDefaultMessage(
        TeamCartViewModel vm, TeamCartNotificationContext context)
    {
        var state = vm.Status.ToString();
        var (title, body) = LocalizeState(state);
        return (title, body);
    }

    private static (string Title, string Body) LocalizeState(string state)
        => state switch
        {
            "Active" => ("Giỏ nhóm đang hoạt động", "Giỏ của bạn đã được cập nhật."),
            "Open" => ("Giỏ nhóm đang mở", "Giỏ của bạn đã được cập nhật."),
            "Locked" => ("Giỏ nhóm đã khóa", "Chủ giỏ đang xác nhận thanh toán."),
            "ReadyToConfirm" => ("Sẵn sàng xác nhận", "Giỏ nhóm đã sẵn sàng để xác nhận thanh toán."),
            "Converted" => ("Đã tạo đơn hàng", "Giỏ nhóm đã được chuyển thành đơn hàng."),
            "Expired" => ("Giỏ nhóm đã hết hạn", "Giỏ nhóm của bạn đã đóng."),
            _ => ("Cập nhật giỏ nhóm", "Giỏ nhóm của bạn đã được cập nhật.")
        };
}
