using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Models;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;

namespace YummyZoom.Application.Users.Commands.RegisterDevice;

public class RegisterDeviceCommandHandler : IRequestHandler<RegisterDeviceCommand, Result>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUserDeviceSessionRepository _userDeviceSessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUser _currentUser;
    private readonly ILogger<RegisterDeviceCommandHandler> _logger; 

    public RegisterDeviceCommandHandler(
        IDeviceRepository deviceRepository,
        IUserDeviceSessionRepository userDeviceSessionRepository,
        IUnitOfWork unitOfWork,
        IUser currentUser,
        ILogger<RegisterDeviceCommandHandler> logger) 
    {
        _deviceRepository = deviceRepository ?? throw new ArgumentNullException(nameof(deviceRepository));
        _userDeviceSessionRepository = userDeviceSessionRepository ?? throw new ArgumentNullException(nameof(userDeviceSessionRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger)); 
    }

    public Task<Result> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(request.Platform) || string.IsNullOrWhiteSpace(request.FcmToken))
            {
                 _logger.LogWarning("RegisterDeviceCommand failed: Missing required device information.");
                 return Result.Failure(UserDeviceErrors.InvalidDeviceData());
            }

            // Use the provided device ID, or null if not provided (per session-based model)
            var deviceId = request.DeviceId?.Trim();

            // Enhanced Device Matching Logic - Try multiple strategies to find the right device
            Device? device = null;

            // Strategy 1: Exact DeviceId match (highest priority - stable device identity)
            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                device = await _deviceRepository.GetByDeviceIdAsync(deviceId, cancellationToken);
                _logger.LogDebug("Strategy 1 - DeviceId lookup for {DeviceId}: {Found}", deviceId, device != null ? "Found" : "Not Found");
            }

            // Strategy 2: FCM token reuse (for app reinstalls, token refresh scenarios)
            UserDeviceSession? existingTokenSession = null;
            if (device == null)
            {
                existingTokenSession = await _userDeviceSessionRepository.GetActiveSessionByTokenAsync(request.FcmToken.Trim(), cancellationToken);
                if (existingTokenSession != null)
                {
                    var candidateDevice = await _deviceRepository.GetByIdAsync(existingTokenSession.DeviceId, cancellationToken);
                    
                    // Only reuse if DeviceId matches or candidate has null DeviceId (compatible scenarios)
                    if (candidateDevice != null && 
                        (candidateDevice.DeviceId == null || candidateDevice.DeviceId == deviceId))
                    {
                        device = candidateDevice;
                        _logger.LogDebug("Strategy 2 - FCM token reuse: Found compatible device {DeviceDbId} with DeviceId {CandidateDeviceId}", 
                            candidateDevice.Id, candidateDevice.DeviceId ?? "null");
                    }
                    else if (candidateDevice != null)
                    {
                        _logger.LogDebug("Strategy 2 - FCM token found incompatible device {DeviceDbId} with DeviceId {CandidateDeviceId}, expected {ExpectedDeviceId}", 
                            candidateDevice.Id, candidateDevice.DeviceId, deviceId ?? "null");
                    }
                }
            }

            if (device == null)
            {
                // Create new device
                device = new Device
                {
                    Id = Guid.NewGuid(),
                    DeviceId = deviceId,
                    Platform = request.Platform.Trim(),
                    ModelName = request.ModelName?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _deviceRepository.AddAsync(device, cancellationToken);
                _logger.LogInformation("Created new Device record for DeviceId {DeviceId}", device.DeviceId);
            }
            else
            {
                // Update existing device info (preserve DeviceId if already set)
                device.Platform = request.Platform.Trim();
                device.ModelName = request.ModelName?.Trim();
                device.UpdatedAt = DateTime.UtcNow;
                
                // Only update DeviceId if the device currently has none and a new one is provided
                if (device.DeviceId == null && !string.IsNullOrWhiteSpace(deviceId))
                {
                    device.DeviceId = deviceId;
                    _logger.LogInformation("Updated Device {DeviceDbId} with new DeviceId {DeviceId}", device.Id, deviceId);
                }
                
                _logger.LogInformation("Updated existing Device record for DeviceId {DeviceId}", device.DeviceId ?? "null");
            }

            // Find and deactivate any existing active session for this FCM token
            // (reuse the session found in Strategy 2 if available, otherwise look it up)
            var sessionToDeactivate = existingTokenSession ?? 
                await _userDeviceSessionRepository.GetActiveSessionByTokenAsync(request.FcmToken.Trim(), cancellationToken);
            
            if (sessionToDeactivate != null)
            {
                sessionToDeactivate.IsActive = false;
                sessionToDeactivate.LoggedOutAt = DateTime.UtcNow;
                _logger.LogInformation("Deactivated existing session for FCM token {FcmToken}", request.FcmToken);
            }

            // Deactivate all previous sessions for this device to ensure only one active session per device
            await _userDeviceSessionRepository.DeactivateSessionsForDeviceAsync(device.Id, cancellationToken);

            // Create a new active session
            var newSession = new UserDeviceSession
            {
                Id = Guid.NewGuid(),
                UserId = _currentUser.DomainUserId!.Value, // Safe to use ! since authorization pipeline guarantees authentication
                DeviceId = device.Id,
                FcmToken = request.FcmToken.Trim(),
                IsActive = true,
                LastLoginAt = DateTime.UtcNow,
                LoggedOutAt = null
            };
            await _userDeviceSessionRepository.AddSessionAsync(newSession, cancellationToken);
            _logger.LogInformation("Created new UserDeviceSession for UserId {UserId} on DeviceId {DeviceId}", _currentUser.DomainUserId.Value, device.DeviceId);

            return Result.Success();
        }, cancellationToken);
    }
}
