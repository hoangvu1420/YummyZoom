using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UserAggregate.Events;

public record UserCreated(UserId UserId) : IDomainEvent;
