using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate.Events;

namespace YummyZoom.Application.Users.EventHandlers;

public class RoleAssignmentAddedToUserEventHandler : INotificationHandler<RoleAssignmentAddedToUserEvent>
{
    private readonly IIdentityService _identityService;

    public RoleAssignmentAddedToUserEventHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(RoleAssignmentAddedToUserEvent notification, CancellationToken cancellationToken)
    {
        // Translate domain RoleAssignment to Identity role string
        var identityRole = notification.RoleAssignment.RoleName;

        // Call IdentityService to add the user to the Identity role
        await _identityService.AddUserToRoleAsync(notification.UserId.Value, identityRole);
    }
}
