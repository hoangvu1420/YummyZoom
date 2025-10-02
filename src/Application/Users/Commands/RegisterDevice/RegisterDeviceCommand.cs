using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Users.Commands.RegisterDevice;

[Authorize]
public record RegisterDeviceCommand(
    string FcmToken,
    string Platform,
    string? DeviceId = null,
    string? ModelName = null) : IRequest<Result>;
