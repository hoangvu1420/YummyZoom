using YummyZoom.Application.Common.Interfaces;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Users.Commands.UnregisterDevice;

public class UnregisterDeviceCommandHandler : IRequestHandler<UnregisterDeviceCommand, Result>
{
    private readonly IUserDeviceRepository _userDeviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUser _currentUser;

    public UnregisterDeviceCommandHandler(
        IUserDeviceRepository userDeviceRepository,
        IUnitOfWork unitOfWork,
        IUser currentUser)
    {
        _userDeviceRepository = userDeviceRepository ?? throw new ArgumentNullException(nameof(userDeviceRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public Task<Result> Handle(UnregisterDeviceCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Ensure user is authenticated
            if (_currentUser.DomainId is null)
            {
                return Result.Failure(UserDeviceErrors.UserNotAuthenticated());
            }

            // Check if token exists before trying to remove it
            var tokenExists = await _userDeviceRepository.TokenExistsAsync(
                request.FcmToken.Trim(), 
                cancellationToken);

            if (!tokenExists)
            {
                return Result.Failure(UserDeviceErrors.TokenNotFound(request.FcmToken));
            }

            // Remove the device token
            await _userDeviceRepository.RemoveTokenAsync(
                request.FcmToken.Trim(),
                cancellationToken);

            return Result.Success();
        }, cancellationToken);
    }
} 
