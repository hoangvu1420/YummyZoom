using YummyZoom.Domain.PayoutAggregate.ValueObjects;

namespace YummyZoom.Domain.PayoutAggregate.Events;

public record PayoutCompleted(PayoutId PayoutId) : DomainEventBase;
