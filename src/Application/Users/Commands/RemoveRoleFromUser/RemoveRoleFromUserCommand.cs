using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Users.Commands.RemoveRoleFromUser;

[Authorize(Roles = Roles.Administrator)]
public record RemoveRoleFromUserCommand(
    Guid UserId,
    string RoleName,
    string? TargetEntityId,
    string? TargetEntityType) : IRequest<Result>;
