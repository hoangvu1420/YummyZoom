using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Notifications.Commands.SendNotificationToUser;

public class SendNotificationToUserCommandHandler : IRequestHandler<SendNotificationToUserCommand, Result>
{
    private readonly IFcmService _fcmService;
    private readonly IUserDeviceRepository _userDeviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendNotificationToUserCommandHandler> _logger;

    public SendNotificationToUserCommandHandler(
        IFcmService fcmService,
        IUserDeviceRepository userDeviceRepository,
        IUnitOfWork unitOfWork,
        ILogger<SendNotificationToUserCommandHandler> logger)
    {
        _fcmService = fcmService ?? throw new ArgumentNullException(nameof(fcmService));
        _userDeviceRepository = userDeviceRepository ?? throw new ArgumentNullException(nameof(userDeviceRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(SendNotificationToUserCommand request, CancellationToken cancellationToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
        {
            return Result.Failure(NotificationErrors.InvalidNotificationData());
        }

        try
        {
            // Convert primitive Guid to UserId value object
            var userId = UserId.Create(request.UserId);

            // Get active FCM tokens for the user
            var fcmTokens = await _userDeviceRepository.GetActiveFcmTokensByUserIdAsync(userId, cancellationToken);

            if (fcmTokens.Count == 0)
            {
                _logger.LogWarning("No active devices found for user {UserId}", request.UserId);
                return Result.Failure(NotificationErrors.NoActiveDevices(userId));
            }

            _logger.LogInformation("Sending notification to {TokenCount} devices for user {UserId}", 
                fcmTokens.Count, request.UserId);

            // Send multicast notification
            var result = await _fcmService.SendMulticastNotificationAsync(
                fcmTokens, 
                request.Title, 
                request.Body, 
                request.DataPayload);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to send notification to user {UserId}: {Error}", 
                    request.UserId, result.Error.Description);
                return Result.Failure(NotificationErrors.FcmSendFailed(result.Error.Description));
            }

            _logger.LogInformation("Successfully sent notification to user {UserId}", request.UserId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while sending notification to user {UserId}", request.UserId);
            return Result.Failure(NotificationErrors.FcmSendFailed($"Unexpected error: {ex.Message}"));
        }
    }
} 
