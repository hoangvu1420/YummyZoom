namespace YummyZoom.Infrastructure.Data;

/// <summary>
/// Infrastructure entity representing a user's device for push notifications.
/// This is not a domain entity as FCM tokens are technical implementation details.
/// </summary>
public class UserDevice
{
    /// <summary>
    /// Primary key for this device record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to ApplicationUser.Id - links to the Infrastructure Identity system
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The Firebase Cloud Messaging token for this device
    /// </summary>
    public string FcmToken { get; set; } = string.Empty;

    /// <summary>
    /// Platform type (e.g., "Android", "iOS", "Web")
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// When this device was first registered
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Last time this device token was used or updated
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Flag to mark tokens as invalid without deleting them immediately.
    /// Useful for error handling from FCM or soft deletion.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Optional device identifier or name for better user experience
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// When this record was last modified
    /// </summary>
    public DateTime UpdatedAt { get; set; }
} 
