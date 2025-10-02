namespace YummyZoom.Infrastructure.Persistence.EfCore.Models;

public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; }
    public string Type { get; init; } = default!;
    public string Content { get; init; } = default!;
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
    public string? AggregateId { get; init; }
    public string? AggregateType { get; init; }

    public int Attempt { get; set; }
    public DateTime? NextAttemptOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }

    public static OutboxMessage FromDomainEvent(
        string eventType,
        string eventContent,
        DateTime nowUtc,
        string? correlationId = null,
        string? causationId = null,
        string? aggregateId = null,
        string? aggregateType = null)
    {
        return new OutboxMessage
        {
            OccurredOnUtc = nowUtc,
            Type = eventType,
            Content = eventContent,
            CorrelationId = correlationId,
            CausationId = causationId,
            AggregateId = aggregateId,
            AggregateType = aggregateType,
            Attempt = 0,
            NextAttemptOnUtc = nowUtc
        };
    }
}


