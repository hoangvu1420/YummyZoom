namespace YummyZoom.Application.Common.Interfaces.IServices;

/// <summary>
/// Notification routing targets for TeamCart push notifications.
/// Supports targeting Host, Members, All, or specific users.
/// </summary>
public enum TeamCartNotificationTarget
{
    /// <summary>
    /// Send notification to all members (host + guests).
    /// </summary>
    All = 0,

    /// <summary>
    /// Send notification to host only.
    /// </summary>
    Host = 1,

    /// <summary>
    /// Send notification to all members except host (guests only).
    /// </summary>
    Members = 2,

    /// <summary>
    /// Send notification to a specific user by ID.
    /// </summary>
    SpecificUser = 3,

    /// <summary>
    /// Send notification to all members except the specified user (actor exclusion).
    /// </summary>
    Others = 4
}

/// <summary>
/// Delivery type for TeamCart push notifications.
/// Controls whether notifications appear in the OS notification tray or are handled silently by the app.
/// </summary>
public enum NotificationDeliveryType
{
    /// <summary>
    /// Hybrid notification (notification + data) - shows in notification tray.
    /// Used for high-value, critical alerts that users need to see immediately.
    /// </summary>
    Hybrid = 0,
    
    /// <summary>
    /// Data-only notification - no notification tray, app handles silently.
    /// Used for low-value, frequent updates that should update UI/badges without cluttering notification tray.
    /// </summary>
    DataOnly = 1
}

/// <summary>
/// Context information for generating event-specific notification messages.
/// </summary>
public record TeamCartNotificationContext
{
    /// <summary>
    /// The type of event that triggered the notification (e.g., "ItemAdded", "MemberJoined").
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// The user ID of the actor who performed the action (for exclusion or personalization).
    /// </summary>
    public Guid? ActorUserId { get; init; }

    /// <summary>
    /// The name of the actor who performed the action.
    /// </summary>
    public string? ActorName { get; init; }

    /// <summary>
    /// The name of the item (for item-related events).
    /// </summary>
    public string? ItemName { get; init; }

    /// <summary>
    /// The quantity (for item-related events).
    /// </summary>
    public int? Quantity { get; init; }

    /// <summary>
    /// The amount (for payment or financial events).
    /// </summary>
    public decimal? Amount { get; init; }

    /// <summary>
    /// The currency (for payment or financial events).
    /// </summary>
    public string? Currency { get; init; }

    /// <summary>
    /// Additional context information (e.g., coupon code, payment method).
    /// </summary>
    public string? AdditionalInfo { get; init; }

    /// <summary>
    /// The order ID (for conversion events).
    /// </summary>
    public Guid? OrderId { get; init; }
}

