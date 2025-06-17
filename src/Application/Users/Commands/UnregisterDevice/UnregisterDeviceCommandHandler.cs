using YummyZoom.Application.Common.Interfaces;
using YummyZoom.SharedKernel;
using Microsoft.Extensions.Logging; 

namespace YummyZoom.Application.Users.Commands.UnregisterDevice;

public class UnregisterDeviceCommandHandler : IRequestHandler<UnregisterDeviceCommand, Result>
{
    private readonly IUserDeviceSessionRepository _userDeviceSessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUser _currentUser;
    private readonly ILogger<UnregisterDeviceCommandHandler> _logger; 

    public UnregisterDeviceCommandHandler(
        IUserDeviceSessionRepository userDeviceSessionRepository,
        IUnitOfWork unitOfWork,
        IUser currentUser,
        ILogger<UnregisterDeviceCommandHandler> logger) 
    {
        _userDeviceSessionRepository = userDeviceSessionRepository ?? throw new ArgumentNullException(nameof(userDeviceSessionRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger)); 
    }

    public Task<Result> Handle(UnregisterDeviceCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Find the active session for the current user and FCM token
            var session = await _userDeviceSessionRepository.GetActiveSessionByTokenAsync(
                request.FcmToken.Trim(),
                cancellationToken);

            if (session == null || session.UserId != _currentUser.DomainUserId!.Value) // Safe to use ! since authorization pipeline guarantees authentication
            {
                // Token not found, or found but belongs to a different user on a shared device
                _logger.LogWarning("UnregisterDeviceCommand failed: FCM token {FcmToken} not found or does not belong to user {UserId}", request.FcmToken, _currentUser.DomainUserId!.Value);
                return Result.Failure(UserDeviceErrors.TokenNotFound(request.FcmToken));
            }

            // Deactivate the session
            session.IsActive = false;
            session.LoggedOutAt = DateTime.UtcNow;
            // EF Core will track the changes, no explicit update call needed for this scenario

            _logger.LogInformation("Deactivated session for UserId {UserId} with FCM token {FcmToken}", _currentUser.DomainUserId!.Value, request.FcmToken);

            return Result.Success();
        }, cancellationToken);
    }
}
