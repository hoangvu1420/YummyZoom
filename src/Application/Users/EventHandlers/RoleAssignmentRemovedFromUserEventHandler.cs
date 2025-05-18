using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate.Events;

namespace YummyZoom.Application.Users.EventHandlers;

public class RoleAssignmentRemovedFromUserEventHandler : INotificationHandler<RoleAssignmentRemovedFromUserEvent>
{
    private readonly IIdentityService _identityService;

    public RoleAssignmentRemovedFromUserEventHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(RoleAssignmentRemovedFromUserEvent notification, CancellationToken cancellationToken)
    {
        // Translate domain RoleAssignment to Identity role string
        var identityRole = notification.RoleAssignment.RoleName;

        // Call IdentityService to remove the user from the Identity role
        await _identityService.RemoveUserFromRoleAsync(notification.UserId.Value, identityRole);
    }
}
