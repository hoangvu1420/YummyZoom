using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Notifications.Commands.SendBroadcastNotification;

[Authorize(Roles = Roles.Administrator)]
public record SendBroadcastNotificationCommand(
    string Title,
    string Body,
    Dictionary<string, string>? DataPayload = null) : IRequest<Result>;
