using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Infrastructure.Notifications.Orders;

/// <summary>
/// Infrastructure implementation that gathers the customer's device tokens
/// and sends a data-only FCM message { type: "order", orderId, version }.
/// </summary>
public sealed class OrderPushNotifier : IOrderPushNotifier
{
    private readonly IUserDeviceSessionRepository _userDeviceSessionRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IFcmService _fcm;
    private readonly ILogger<OrderPushNotifier> _logger;

    public OrderPushNotifier(
        IUserDeviceSessionRepository userDeviceSessionRepository,
        IOrderRepository orderRepository,
        IFcmService fcm,
        ILogger<OrderPushNotifier> logger)
    {
        _userDeviceSessionRepository = userDeviceSessionRepository;
        _orderRepository = orderRepository;
        _fcm = fcm;
        _logger = logger;
    }

    public async Task<Result> PushOrderDataAsync(Guid orderId, Guid customerUserId, long version, CancellationToken cancellationToken = default)
    {
        var tokens = await _userDeviceSessionRepository.GetActiveFcmTokensByUserIdAsync(customerUserId, cancellationToken);
        if (tokens.Count == 0)
        {
            _logger.LogWarning("No active device tokens; skipping Order FCM push (OrderId={OrderId}, UserId={UserId})", orderId, customerUserId);
            return Result.Success();
        }
        // Filter out any seed tokens before sending notifications
        var validTokens = tokens.Where(token => !token.StartsWith("seedToken")).ToList();
        if (validTokens.Count == 0)
        {
            _logger.LogDebug("All tokens are seed tokens; skipping Order FCM push (OrderId={OrderId}, UserId={UserId})", orderId, customerUserId);
            return Result.Success();
        }
        // Update tokens to use only valid ones
        tokens = validTokens;

        // Load order to enrich payload
        var order = await _orderRepository.GetByIdAsync(OrderId.Create(orderId), cancellationToken);
        if (order is null)
        {
            _logger.LogWarning("Order not found when pushing FCM (OrderId={OrderId}). Skipping.", orderId);
            return Result.Success();
        }

        var status = order.Status.ToString(); // Keep source casing
        var (title, body) = LocalizeStatus(status);
        var route = $"/orders/{orderId}";
        var message = !string.IsNullOrWhiteSpace(order.OrderNumber)
            ? $"Đơn hàng #{order.OrderNumber} {TrimTrailingPeriod(body)}"
            : body;

        var data = new Dictionary<string, string>
        {
            ["type"] = "order",
            ["orderId"] = orderId.ToString(),
            ["version"] = version.ToString(),
            ["status"] = status,
            ["title"] = title,
            ["body"] = body,
            ["message"] = message,
            ["route"] = route
        };

        var push = await _fcm.SendMulticastDataAsync(tokens, data);
        if (push.IsFailure)
        {
            _logger.LogError("Failed to send Order FCM data push (OrderId={OrderId}): {Error}", orderId, push.Error);
            return Result.Failure(push.Error);
        }

        _logger.LogInformation("Sent Order FCM data push to {Count} tokens (OrderId={OrderId}, Status={Status}, Version={Version})",
            tokens.Count, orderId, status, version);
        return Result.Success();
    }

    private static (string Title, string Body) LocalizeStatus(string status)
        => status switch
        {
            // Explicit mappings provided by Mobile team
            "Accepted" => ("Xác nhận", "Đơn hàng của bạn đã được xác nhận."),
            "Preparing" => ("Chuẩn bị", "Đơn hàng của bạn đang được nhà hàng chuẩn bị."),
            "ReadyForDelivery" => ("Giao món", "Đơn hàng của bạn đã sẵn sàng để giao."),
            "Delivered" => ("Hoàn thành", "Đơn hàng của bạn đã được giao."),

            // Additional reasonable mappings
            "AwaitingPayment" => ("Chờ thanh toán", "Đơn hàng của bạn đang chờ thanh toán."),
            "Placed" => ("Đã đặt hàng", "Đơn hàng của bạn đã được tạo."),
            "Cancelled" => ("Đã hủy", "Đơn hàng của bạn đã bị hủy."),
            "Rejected" => ("Bị từ chối", "Nhà hàng đã từ chối đơn hàng của bạn."),

            // Fallback
            _ => ("Cập nhật đơn hàng", "Đơn hàng của bạn đã được cập nhật.")
        };

    private static string TrimTrailingPeriod(string text)
        => string.IsNullOrWhiteSpace(text) ? text : text.TrimEnd().TrimEnd('.', '。', '!');
}
