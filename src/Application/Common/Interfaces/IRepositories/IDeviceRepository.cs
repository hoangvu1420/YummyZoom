using YummyZoom.SharedKernel.Models;

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
    /// Adds a new device to the repository.
    /// </summary>
    /// <param name="device">The Device entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(Device device, CancellationToken cancellationToken = default);
}
