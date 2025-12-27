using YummyZoom.Domain.PayoutAggregate.ValueObjects;

namespace YummyZoom.Domain.PayoutAggregate.Events;

public record PayoutFailed(
    PayoutId PayoutId,
    string Reason) : DomainEventBase;
