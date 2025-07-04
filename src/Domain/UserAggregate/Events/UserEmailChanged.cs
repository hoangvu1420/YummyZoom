using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UserAggregate.Events;

public record UserEmailChanged(UserId UserId, string OldEmail, string NewEmail) : IDomainEvent;
