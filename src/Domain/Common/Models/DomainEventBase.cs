namespace YummyZoom.Domain.Common.Models;

public abstract record DomainEventBase : IDomainEvent, IHasEventId
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
