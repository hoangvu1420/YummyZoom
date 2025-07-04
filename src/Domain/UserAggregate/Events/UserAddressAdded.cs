using YummyZoom.Domain.UserAggregate.Entities;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UserAggregate.Events;

public record UserAddressAdded(UserId UserId, Address Address) : IDomainEvent;
