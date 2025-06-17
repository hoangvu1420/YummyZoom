using YummyZoom.SharedKernel;
using YummyZoom.Application.Common.Security;

namespace YummyZoom.Application.Users.Commands.UnregisterDevice;

[Authorize]
public record UnregisterDeviceCommand(
    string FcmToken) : IRequest<Result>; 
