using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Users.Commands.RegisterDevice;

public record RegisterDeviceCommand(
    string FcmToken,
    string Platform,
    string? DeviceId = null,
    string? ModelName = null) : IRequest<Result>;
