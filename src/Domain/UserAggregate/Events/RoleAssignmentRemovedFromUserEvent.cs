using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UserAggregate.Events;

public record RoleAssignmentRemovedFromUserEvent(UserId UserId, RoleAssignment RoleAssignment) : IDomainEvent;
