using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UserAggregate.Events;

public record UserDefaultPaymentMethodChanged(UserId UserId, PaymentMethodId PaymentMethodId) : IDomainEvent;
