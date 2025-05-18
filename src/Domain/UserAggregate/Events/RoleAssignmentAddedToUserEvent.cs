using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UserAggregate.Events;

public record RoleAssignmentAddedToUserEvent(UserId UserId, RoleAssignment RoleAssignment) : IDomainEvent;
