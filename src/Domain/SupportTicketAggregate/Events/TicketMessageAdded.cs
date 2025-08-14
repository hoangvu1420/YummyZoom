using YummyZoom.Domain.SupportTicketAggregate.Enums;
using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;

namespace YummyZoom.Domain.SupportTicketAggregate.Events;

public record TicketMessageAdded(
    SupportTicketId SupportTicketId,
    MessageId MessageId,
    Guid AuthorId,
    AuthorType AuthorType,
    string MessageText,
    bool IsInternalNote) : DomainEventBase;
