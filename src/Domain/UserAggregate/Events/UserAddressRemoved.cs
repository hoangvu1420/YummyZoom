using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UserAggregate.Events;

public record UserAddressRemoved(UserId UserId, AddressId AddressId) : DomainEventBase;
