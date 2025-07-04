using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.SupportTicketAggregate.Errors;

public static class SupportTicketErrors
{
    // Value Object Validation Errors
    public static Error InvalidSupportTicketId(string value) => Error.Validation(
        "SupportTicket.InvalidSupportTicketId",
        $"Support ticket ID '{value}' is not a valid GUID.");

    public static Error InvalidMessageId(string value) => Error.Validation(
        "SupportTicket.InvalidMessageId",
        $"Message ID '{value}' is not a valid GUID.");

    public static Error InvalidTicketNumber(string reason) => Error.Validation(
        "SupportTicket.InvalidTicketNumber",
        $"Ticket number is invalid: {reason}");

    public static Error InvalidContextEntityId(string reason) => Error.Validation(
        "SupportTicket.InvalidContextEntityId",
        $"Context entity ID is invalid: {reason}");

    // Business Rule Errors
    public static Error SupportTicketNotFound(Guid ticketId) => Error.NotFound(
        "SupportTicket.SupportTicketNotFound",
        $"Support ticket with ID '{ticketId}' not found.");

    public static Error InvalidSubject(string reason) => Error.Validation(
        "SupportTicket.InvalidSubject",
        $"Subject is invalid: {reason}");

    public static Error NoContextLinksProvided => Error.Validation(
        "SupportTicket.NoContextLinksProvided",
        "A support ticket must have at least one context link to be meaningful.");

    public static Error MessageNotFound(Guid messageId) => Error.NotFound(
        "SupportTicket.MessageNotFound",
        $"Message with ID '{messageId}' not found in the ticket.");

    public static Error InvalidMessageText(string reason) => Error.Validation(
        "SupportTicket.InvalidMessageText",
        $"Message text is invalid: {reason}");

    public static Error InvalidStatusTransition(string currentStatus, string newStatus) => Error.Validation(
        "SupportTicket.InvalidStatusTransition",
        $"Cannot transition from '{currentStatus}' to '{newStatus}'.");

    public static Error UnauthorizedStatusChange(string status) => Error.Validation(
        "SupportTicket.UnauthorizedStatusChange",
        $"Only admins can change status to '{status}'.");

    public static Error InvalidAdminId(string reason) => Error.Validation(
        "SupportTicket.InvalidAdminId",
        $"Admin ID is invalid: {reason}");

    public static Error InvalidAuthorId(string reason) => Error.Validation(
        "SupportTicket.InvalidAuthorId",
        $"Author ID is invalid: {reason}");

    public static Error TicketAlreadyAssigned(Guid currentAdminId) => Error.Conflict(
        "SupportTicket.TicketAlreadyAssigned",
        $"Ticket is already assigned to admin '{currentAdminId}'.");

    public static Error TicketCreationFailed(string reason) => Error.Failure(
        "SupportTicket.TicketCreationFailed",
        $"Failed to create support ticket: {reason}");

    public static Error TicketUpdateFailed(string reason) => Error.Failure(
        "SupportTicket.TicketUpdateFailed",
        $"Failed to update support ticket: {reason}");
}
