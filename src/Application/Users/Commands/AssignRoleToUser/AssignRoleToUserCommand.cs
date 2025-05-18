using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Users.Commands.AssignRoleToUser;

[Authorize(Roles = Roles.Administrator)]
public record AssignRoleToUserCommand(
    Guid UserId,
    string RoleName,
    string? TargetEntityId,
    string? TargetEntityType) : IRequest<Result>;
