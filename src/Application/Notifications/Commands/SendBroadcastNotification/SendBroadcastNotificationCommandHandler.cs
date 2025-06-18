using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Notifications.Commands.SendBroadcastNotification;

public class SendBroadcastNotificationCommandHandler : IRequestHandler<SendBroadcastNotificationCommand, Result>
{
    private readonly IFcmService _fcmService;
    private readonly IUserDeviceSessionRepository _userDeviceSessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendBroadcastNotificationCommandHandler> _logger;

    public SendBroadcastNotificationCommandHandler(
        IFcmService fcmService,
        IUserDeviceSessionRepository userDeviceSessionRepository,
        IUnitOfWork unitOfWork,
        ILogger<SendBroadcastNotificationCommandHandler> logger)
    {
        _fcmService = fcmService ?? throw new ArgumentNullException(nameof(fcmService));
        _userDeviceSessionRepository = userDeviceSessionRepository ?? throw new ArgumentNullException(nameof(userDeviceSessionRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(SendBroadcastNotificationCommand request, CancellationToken cancellationToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
        {
            return Result.Failure(NotificationErrors.InvalidNotificationData());
        }

        try
        {
            // Get all active FCM tokens from all users from sessions
            var fcmTokens = await _userDeviceSessionRepository.GetAllActiveFcmTokensAsync(cancellationToken);

            if (fcmTokens.Count == 0)
            {
                _logger.LogWarning("No active devices found for broadcast notification");
                return Result.Failure(NotificationErrors.NoActiveDevicesForBroadcast());
            }

            _logger.LogInformation("Sending broadcast notification to {TokenCount} devices", fcmTokens.Count);

            // Send multicast notification to all devices
            var result = await _fcmService.SendMulticastNotificationAsync(
                fcmTokens,
                request.Title,
                request.Body,
                request.DataPayload);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to send broadcast notification: {Error}", result.Error.Description);
                return Result.Failure(NotificationErrors.BroadcastFailed(result.Error.Description));
            }

            _logger.LogInformation("Successfully sent broadcast notification to all active devices");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while sending broadcast notification");
            return Result.Failure(NotificationErrors.BroadcastFailed($"Unexpected error: {ex.Message}"));
        }
    }
}
