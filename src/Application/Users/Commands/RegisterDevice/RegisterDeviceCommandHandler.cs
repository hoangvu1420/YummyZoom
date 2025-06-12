using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Users.Commands.RegisterDevice;

public class RegisterDeviceCommandHandler : IRequestHandler<RegisterDeviceCommand, Result>
{
    private readonly IUserDeviceRepository _userDeviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUser _currentUser;

    public RegisterDeviceCommandHandler(
        IUserDeviceRepository userDeviceRepository,
        IUnitOfWork unitOfWork,
        IUser currentUser)
    {
        _userDeviceRepository = userDeviceRepository ?? throw new ArgumentNullException(nameof(userDeviceRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public Task<Result> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Ensure user is authenticated
            if (_currentUser.DomainId is null)
            {
                return Result.Failure(UserDeviceErrors.UserNotAuthenticated());
            }

            // Register or update the device
            await _userDeviceRepository.AddOrUpdateAsync(
                _currentUser.DomainId,
                request.FcmToken.Trim(),
                request.Platform.Trim(),
                request.DeviceId?.Trim(),
                cancellationToken);

            return Result.Success();
        }, cancellationToken);
    }
} 
