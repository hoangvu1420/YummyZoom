using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UserAggregate.Events;

public record UserProfileUpdated(UserId UserId, string Name, string? PhoneNumber) : DomainEventBase;
