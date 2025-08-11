using YummyZoom.Application.Common.Models;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IDeviceRepository
{
    /// <summary>
    /// Finds a device by its unique OS-provided DeviceId.
    /// </summary>
    /// <param name="deviceId">The unique OS-provided device identifier. Can be null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Device entity if found, otherwise null. Returns null if deviceId is null.</returns>
    Task<Device?> GetByDeviceIdAsync(string? deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a device by its database ID.
    /// </summary>
    /// <param name="id">The database ID of the device.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Device entity if found, otherwise null.</returns>
    Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates and persists a new device using the provided primitive parameters.
    /// </summary>
    /// <param name="deviceId">The stable, unique identifier from the OS. Can be null.</param>
    /// <param name="platform">Platform type (e.g., "Android", "iOS", "Web").</param>
    /// <param name="modelName">Optional device model name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created Device entity.</returns>
    Task<Device> AddAsync(string? deviceId, string platform, string? modelName, CancellationToken cancellationToken = default);
}
