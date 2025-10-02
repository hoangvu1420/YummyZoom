using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Users.Commands.UnregisterDevice;

[Authorize]
public record UnregisterDeviceCommand(
    string FcmToken) : IRequest<Result>;
