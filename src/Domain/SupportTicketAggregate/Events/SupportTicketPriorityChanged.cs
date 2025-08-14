using YummyZoom.Domain.SupportTicketAggregate.Enums;
using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;

namespace YummyZoom.Domain.SupportTicketAggregate.Events;

public record SupportTicketPriorityChanged(
    SupportTicketId SupportTicketId,
    SupportTicketPriority PreviousPriority,
    SupportTicketPriority NewPriority,
    Guid ChangedByAdminId) : DomainEventBase;
