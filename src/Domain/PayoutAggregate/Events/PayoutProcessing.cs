using YummyZoom.Domain.PayoutAggregate.ValueObjects;

namespace YummyZoom.Domain.PayoutAggregate.Events;

public record PayoutProcessing(
    PayoutId PayoutId,
    string? ProviderReferenceId) : DomainEventBase;
