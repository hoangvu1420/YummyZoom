using System.ComponentModel.DataAnnotations;

namespace YummyZoom.SharedKernel.Models;

/// <summary>
/// Infrastructure entity representing a physical device.
/// </summary>
public class Device
{
    /// <summary>
    /// Primary key for this device record
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The stable, unique identifier from the OS.
    /// Can be null if the device doesn't provide a stable identifier.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Platform type (e.g., "Android", "iOS", "Web")
    /// </summary>
    [Required]
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Optional device model name for analytics
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// When this device record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this device record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
