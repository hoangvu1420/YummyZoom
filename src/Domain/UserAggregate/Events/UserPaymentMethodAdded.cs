using YummyZoom.Domain.UserAggregate.Entities;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UserAggregate.Events;

public record UserPaymentMethodAdded(UserId UserId, PaymentMethod PaymentMethod) : IDomainEvent;
