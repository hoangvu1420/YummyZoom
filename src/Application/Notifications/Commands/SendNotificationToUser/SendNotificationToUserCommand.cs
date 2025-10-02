using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Notifications.Commands.SendNotificationToUser;

[Authorize(Roles = Roles.Administrator)]
public record SendNotificationToUserCommand(
    Guid UserId,
    string Title,
    string Body,
    Dictionary<string, string>? DataPayload = null) : IRequest<Result>;
