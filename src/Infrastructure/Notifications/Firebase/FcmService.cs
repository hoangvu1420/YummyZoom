using FirebaseAdmin.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.SharedKernel;

namespace YummyZoom.Infrastructure.Notifications.Firebase;

public class FcmService : IFcmService
{
    private readonly ILogger<FcmService> _logger;
    private readonly FirebaseMessaging _firebaseMessaging;
    private readonly IServiceProvider _serviceProvider;

    public FcmService(
        ILogger<FcmService> logger,
        FirebaseMessaging firebaseMessaging,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _firebaseMessaging = firebaseMessaging ?? throw new ArgumentNullException(nameof(firebaseMessaging));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    // Helper method to mark a token as invalid using the new repository
    private async Task<Result> MarkTokenAsInvalidAsync(string fcmToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var userDeviceSessionRepository = scope.ServiceProvider.GetRequiredService<IUserDeviceSessionRepository>();

            var session = await userDeviceSessionRepository.GetActiveSessionByTokenAsync(fcmToken);
            if (session != null)
            {
                session.IsActive = false;
                session.LoggedOutAt = DateTime.UtcNow;
                
                _logger.LogInformation("Marked FCM token {FcmTokenPrefix} as invalid", 
                    fcmToken[..Math.Min(8, fcmToken.Length)]);
                
                return Result.Success();
            }
            
            _logger.LogWarning("No active session found for FCM token {FcmTokenPrefix} to mark as invalid", 
                fcmToken[..Math.Min(8, fcmToken.Length)]);
            
            return Result.Success(); // Not finding a session to invalidate is not an error
        });
    }

    public async Task<Result> SendNotificationAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null)
    {
        try
        {
            var message = new Message
            {
                Token = fcmToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = data,
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        ChannelId = "yummyzoom_channel"
                    }
                },
                Apns = new ApnsConfig
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "apns-priority", "10" }
                    },
                    Aps = new Aps
                    {
                        Sound = "default"
                    }
                }
            };

            string response = await _firebaseMessaging.SendAsync(message);
            _logger.LogInformation("Successfully sent message to token {FcmTokenPrefix}. Response: {Response}",
                fcmToken[..Math.Min(8, fcmToken.Length)], response);
            return Result.Success();
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Failed to send FCM message to token {FcmTokenPrefix}. Error Code: {ErrorCode}",
                fcmToken[..Math.Min(8, fcmToken.Length)], ex.MessagingErrorCode);

            // Handle invalid tokens
            if (ex.MessagingErrorCode == MessagingErrorCode.Unregistered || ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
            {
                var markResult = await MarkTokenAsInvalidAsync(fcmToken);
                if (markResult.IsFailure)
                {
                    _logger.LogError("Failed to mark FCM token as invalid: {Error}", markResult.Error);
                }

                _logger.LogWarning("Marked FCM token {FcmTokenPrefix} as invalid due to error code {ErrorCode}.",
                    fcmToken[..Math.Min(8, fcmToken.Length)], ex.MessagingErrorCode);
            }

            return Result.Failure(Error.Failure(
                "FcmService.SendFailed",
                $"FCM send failed: {ex.Message} (Code: {ex.MessagingErrorCode})"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while sending FCM message to token {FcmTokenPrefix}.",
                fcmToken[..Math.Min(8, fcmToken.Length)]);
            return Result.Failure(Error.Failure(
                "FcmService.UnexpectedError",
                $"Unexpected FCM error: {ex.Message}"));
        }
    }

    public async Task<Result> SendDataMessageAsync(string fcmToken, Dictionary<string, string> data)
    {
        try
        {
            var message = new Message
            {
                Token = fcmToken,
                Data = data
            };

            string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("Successfully sent data message to token {FcmTokenPrefix}. Response: {Response}",
                fcmToken[..Math.Min(8, fcmToken.Length)], response);
            return Result.Success();
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Failed to send FCM data message to token {FcmTokenPrefix}. Error Code: {ErrorCode}",
                fcmToken[..Math.Min(8, fcmToken.Length)], ex.MessagingErrorCode);

            if (ex.MessagingErrorCode == MessagingErrorCode.Unregistered || ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
            {
                var markResult = await MarkTokenAsInvalidAsync(fcmToken);
                if (markResult.IsFailure)
                {
                    _logger.LogError("Failed to mark FCM token as invalid: {Error}", markResult.Error);
                }

                _logger.LogWarning("Marked FCM token {FcmTokenPrefix} as invalid due to error code {ErrorCode}.",
                    fcmToken[..Math.Min(8, fcmToken.Length)], ex.MessagingErrorCode);
            }

            return Result.Failure(Error.Failure(
                "FcmService.DataSendFailed",
                $"FCM data send failed: {ex.Message} (Code: {ex.MessagingErrorCode})"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while sending FCM data message to token {FcmTokenPrefix}.",
                fcmToken[..Math.Min(8, fcmToken.Length)]);
            return Result.Failure(Error.Failure(
                "FcmService.UnexpectedDataError",
                $"Unexpected FCM data error: {ex.Message}"));
        }
    }

    public async Task<Result<List<string>>> SendMulticastNotificationAsync(IEnumerable<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null)
    {
        var tokensList = fcmTokens.ToList();
        if (!tokensList.Any())
        {
            _logger.LogWarning("Attempted to send multicast notification with no tokens provided.");
            return Result.Success<List<string>>([]); // No tokens to send to, consider it success
        }

        try
        {
            var multicastMessage = new MulticastMessage
            {
                Tokens = tokensList,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = data,
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        ChannelId = "yummyzoom_channel"
                    }
                },
                Apns = new ApnsConfig
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "apns-priority", "10" }
                    },
                    Aps = new Aps
                    {
                        Sound = "default"
                    }
                }
            };

            var response = await _firebaseMessaging.SendEachForMulticastAsync(multicastMessage);

            if (response.FailureCount > 0)
            {
                var failedTokens = new List<string>();
                for (int i = 0; i < response.Responses.Count; i++)
                {
                    var sendResponse = response.Responses[i];
                    if (!sendResponse.IsSuccess)
                    {
                        failedTokens.Add(tokensList[i]);
                        _logger.LogError(sendResponse.Exception, "Failed to send message to token {FcmTokenPrefix}. Error: {ErrorMessage}",
                            tokensList[i][..Math.Min(8, tokensList[i].Length)], sendResponse.Exception?.Message);

                        // Mark individual tokens as invalid if the error is specific
                        if (sendResponse.Exception is FirebaseMessagingException fmEx &&
                            (fmEx.MessagingErrorCode == MessagingErrorCode.Unregistered || fmEx.MessagingErrorCode == MessagingErrorCode.InvalidArgument))
                        {
                            var markResult = await MarkTokenAsInvalidAsync(tokensList[i]);
                            if (markResult.IsFailure)
                            {
                                _logger.LogError("Failed to mark FCM token as invalid: {Error}", markResult.Error);
                            }

                            _logger.LogWarning("Marked FCM token {FcmTokenPrefix} as invalid due to error code {ErrorCode}.",
                                tokensList[i][..Math.Min(8, tokensList[i].Length)], fmEx.MessagingErrorCode);
                        }
                    }
                }
                return Result.Failure<List<string>>(Error.Failure(
                    "FcmService.MulticastSendFailed",
                    $"Multicast FCM send failed for {response.FailureCount} tokens."));
            }

            _logger.LogInformation("Successfully sent multicast message to {SuccessCount} tokens.", response.SuccessCount);
            return Result.Success<List<string>>([]); // All succeeded, no failed tokens
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while sending multicast FCM message.");
            return Result.Failure<List<string>>(Error.Failure(
                "FcmService.UnexpectedMulticastError",
                $"Unexpected multicast FCM error: {ex.Message}"));
        }
    }
}
