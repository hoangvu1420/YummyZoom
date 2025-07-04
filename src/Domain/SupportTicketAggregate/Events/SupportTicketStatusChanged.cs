using YummyZoom.Domain.SupportTicketAggregate.Enums;
using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;

namespace YummyZoom.Domain.SupportTicketAggregate.Events;

public record SupportTicketStatusChanged(
    SupportTicketId SupportTicketId,
    SupportTicketStatus PreviousStatus,
    SupportTicketStatus NewStatus,
    Guid? ChangedByAdminId = null) : IDomainEvent;
