using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UserAggregate.Events;

public record UserDeleted(UserId UserId) : DomainEventBase;
