using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace YummyZoom.Infrastructure.Persistence.Repositories;

public class UserDeviceRepository : IUserDeviceRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserDeviceRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddOrUpdateAsync(UserId userId, string fcmToken, string platform, string? deviceId = null, CancellationToken cancellationToken = default)
    {
        var existingDevice = await _dbContext.UserDevices
            .FirstOrDefaultAsync(ud => ud.UserId == userId.Value && ud.FcmToken == fcmToken, cancellationToken);

        if (existingDevice == null)
        {
            // Add new device
            var newDevice = new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                FcmToken = fcmToken,
                Platform = platform,
                DeviceId = deviceId,
                RegisteredAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _dbContext.UserDevices.AddAsync(newDevice, cancellationToken);
        }
        else
        {
            // Update existing device
            existingDevice.Platform = platform;
            existingDevice.DeviceId = deviceId;
            existingDevice.LastUsedAt = DateTime.UtcNow;
            existingDevice.UpdatedAt = DateTime.UtcNow;
            existingDevice.IsActive = true; // Reactivate if it was marked inactive

            _dbContext.UserDevices.Update(existingDevice);
        }

        // Note: SaveChangesAsync is typically called by the Unit of Work pattern
        // or explicitly after the command handler completes its operations
    }

    public async Task<List<string>> GetActiveFcmTokensByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserDevices
            .Where(ud => ud.UserId == userId.Value && ud.IsActive)
            .Select(ud => ud.FcmToken)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetAllActiveFcmTokensAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserDevices
            .Where(ud => ud.IsActive)
            .Select(ud => ud.FcmToken)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkTokenAsInvalidAsync(string fcmToken, CancellationToken cancellationToken = default)
    {
        var device = await _dbContext.UserDevices
            .FirstOrDefaultAsync(ud => ud.FcmToken == fcmToken, cancellationToken);

        if (device != null)
        {
            device.IsActive = false;
            device.UpdatedAt = DateTime.UtcNow;
            _dbContext.UserDevices.Update(device);
        }
    }

    public async Task RemoveTokenAsync(string fcmToken, CancellationToken cancellationToken = default)
    {
        var device = await _dbContext.UserDevices
            .FirstOrDefaultAsync(ud => ud.FcmToken == fcmToken, cancellationToken);

        if (device != null)
        {
            _dbContext.UserDevices.Remove(device);
        }
    }

    public async Task RemoveAllTokensForUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var userDevices = await _dbContext.UserDevices
            .Where(ud => ud.UserId == userId.Value)
            .ToListAsync(cancellationToken);

        if (userDevices.Count > 0)
        {
            _dbContext.UserDevices.RemoveRange(userDevices);
        }
    }

    public async Task<bool> TokenExistsAsync(string fcmToken, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserDevices
            .AnyAsync(ud => ud.FcmToken == fcmToken && ud.IsActive, cancellationToken);
    }
} 
