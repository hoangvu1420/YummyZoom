using YummyZoom.Domain.SupportTicketAggregate.Enums;
using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;

namespace YummyZoom.Domain.SupportTicketAggregate.Events;

public record SupportTicketCreated(
    SupportTicketId SupportTicketId,
    TicketNumber TicketNumber,
    string Subject,
    SupportTicketType Type,
    SupportTicketPriority Priority,
    IReadOnlyList<ContextLink> ContextLinks) : DomainEventBase;
