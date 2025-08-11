using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Models;

namespace YummyZoom.Infrastructure.Data.Repositories;

public class DeviceRepository : IDeviceRepository
{
    private readonly ApplicationDbContext _context;

    public DeviceRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Device?> GetByDeviceIdAsync(string? deviceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return null;
            
        return await _context.Devices
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId, cancellationToken);
    }

    public async Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<Device> AddAsync(string? deviceId, string platform, string? modelName, CancellationToken cancellationToken = default)
    {
        var device = new Device
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            Platform = platform,
            ModelName = modelName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Devices.AddAsync(device, cancellationToken);
        return device;
    }
}
