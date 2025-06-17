using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.SharedKernel.Models;

namespace YummyZoom.Infrastructure.Data.Repositories;

public class UserDeviceSessionRepository : IUserDeviceSessionRepository
{
    private readonly ApplicationDbContext _context;

    public UserDeviceSessionRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task AddSessionAsync(UserDeviceSession session, CancellationToken cancellationToken = default)
    {
        await _context.UserDeviceSessions.AddAsync(session, cancellationToken);
    }

    public async Task DeactivateSessionsForDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        await _context.UserDeviceSessions
            .Where(s => s.DeviceId == deviceId && s.IsActive)
            .ExecuteUpdateAsync(s => s
                .SetProperty(session => session.IsActive, false)
                .SetProperty(session => session.LoggedOutAt, DateTime.UtcNow),
                cancellationToken);
    }

    public async Task<List<string>> GetActiveFcmTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserDeviceSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .Select(s => s.FcmToken)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDeviceSession?> GetActiveSessionByTokenAsync(string fcmToken, CancellationToken cancellationToken = default)
    {
        return await _context.UserDeviceSessions
            .FirstOrDefaultAsync(s => s.FcmToken == fcmToken && s.IsActive, cancellationToken);
    }

    public async Task<List<string>> GetAllActiveFcmTokensAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UserDeviceSessions
            .Where(s => s.IsActive)
            .Select(s => s.FcmToken)
            .ToListAsync(cancellationToken);
    }
}
