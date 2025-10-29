using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Interfaces.IServices;

/// <summary>
/// Sends FCM data-only pushes for Order changes to the customer’s active devices.
/// Payload: { type: "order", orderId, version }
/// </summary>
public interface IOrderPushNotifier
{
    Task<Result> PushOrderDataAsync(Guid orderId, Guid customerUserId, long version, CancellationToken cancellationToken = default);
}

