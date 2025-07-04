using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;

namespace YummyZoom.Domain.SupportTicketAggregate.Events;

public record SupportTicketAssigned(
    SupportTicketId SupportTicketId,
    Guid AssignedToAdminId,
    Guid? PreviousAdminId = null) : IDomainEvent;
