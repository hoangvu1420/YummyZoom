using YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;

namespace YummyZoom.Domain.RoleAssignmentAggregate.Events;

public record RoleAssignmentDeleted(RoleAssignmentId RoleAssignmentId) : IDomainEvent;
