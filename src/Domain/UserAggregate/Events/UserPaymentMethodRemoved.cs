using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UserAggregate.Events;

public record UserPaymentMethodRemoved(UserId UserId, PaymentMethodId PaymentMethodId) : DomainEventBase;
