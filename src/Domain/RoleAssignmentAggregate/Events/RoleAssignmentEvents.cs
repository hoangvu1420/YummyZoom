using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.RoleAssignmentAggregate.Events;

public record RoleAssignmentCreated(
    RoleAssignmentId RoleAssignmentId,
    UserId UserId,
    RestaurantId RestaurantId,
    RestaurantRole Role) : IDomainEvent;

public record RoleAssignmentRemoved(
    RoleAssignmentId RoleAssignmentId,
    UserId UserId,
    RestaurantId RestaurantId,
    RestaurantRole Role) : IDomainEvent;
