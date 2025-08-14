using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.RoleAssignmentAggregate.Events;

public record RoleAssignmentUpdated(
    RoleAssignmentId RoleAssignmentId,
    UserId UserId,
    RestaurantId RestaurantId,
    RestaurantRole PreviousRole,
    RestaurantRole NewRole) : DomainEventBase;
