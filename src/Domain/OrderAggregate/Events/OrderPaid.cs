using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Domain.OrderAggregate.Events;

public record OrderPaid(OrderId OrderId, PaymentTransactionId PaymentTransactionId) : DomainEventBase;
