using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.SharedKernel;

namespace YummyZoom.Infrastructure.Notifications.Orders;

/// <summary>
/// Infrastructure implementation that gathers the customer's device tokens
/// and sends a data-only FCM message { type: "order", orderId, version }.
/// </summary>
public sealed class OrderPushNotifier : IOrderPushNotifier
{
    private readonly IUserDeviceSessionRepository _userDeviceSessionRepository;
    private readonly IFcmService _fcm;
    private readonly ILogger<OrderPushNotifier> _logger;

    public OrderPushNotifier(
        IUserDeviceSessionRepository userDeviceSessionRepository,
        IFcmService fcm,
        ILogger<OrderPushNotifier> logger)
    {
        _userDeviceSessionRepository = userDeviceSessionRepository;
        _fcm = fcm;
        _logger = logger;
    }

    public async Task<Result> PushOrderDataAsync(Guid orderId, Guid customerUserId, long version, CancellationToken cancellationToken = default)
    {
        var tokens = await _userDeviceSessionRepository.GetActiveFcmTokensByUserIdAsync(customerUserId, cancellationToken);
        if (tokens.Count == 0)
        {
            _logger.LogDebug("No active device tokens; skipping Order FCM push (OrderId={OrderId}, UserId={UserId})", orderId, customerUserId);
            return Result.Success();
        }

        var data = new Dictionary<string, string>
        {
            ["type"] = "order",
            ["orderId"] = orderId.ToString(),
            ["version"] = version.ToString()
        };

        var push = await _fcm.SendMulticastDataAsync(tokens, data);
        if (push.IsFailure)
        {
            _logger.LogError("Failed to send Order FCM data push (OrderId={OrderId}): {Error}", orderId, push.Error);
            return Result.Failure(push.Error);
        }

        _logger.LogInformation("Sent Order FCM data push to {Count} tokens (OrderId={OrderId}, Version={Version})",
            tokens.Count, orderId, version);
        return Result.Success();
    }
}

