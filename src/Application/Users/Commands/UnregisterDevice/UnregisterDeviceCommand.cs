using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Users.Commands.UnregisterDevice;

public record UnregisterDeviceCommand(
    string FcmToken) : IRequest<Result>; 
