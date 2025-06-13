namespace YummyZoom.SharedKernel.Models;

/// <summary>
/// Infrastructure entity representing a user's session on a specific device.
/// </summary>
public class UserDeviceSession
{
    /// <summary>
    /// Primary key for this session record
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the ApplicationUser (Identity system)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Foreign key to the Device
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// The active Firebase Cloud Messaging token for this specific session
    /// </summary>
    public string FcmToken { get; set; } = string.Empty;

    /// <summary>
    /// Crucial flag: true if this is a current, valid session for push notifications
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Timestamp for when this session became active (user logged in)
    /// </summary>
    public DateTime LastLoginAt { get; set; }

    /// <summary>
    /// Timestamp for when the user explicitly logged out
    /// </summary>
    public DateTime? LoggedOutAt { get; set; }
}
