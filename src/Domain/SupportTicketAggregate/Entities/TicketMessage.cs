using YummyZoom.Domain.SupportTicketAggregate.Enums;
using YummyZoom.Domain.SupportTicketAggregate.Errors;
using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.SupportTicketAggregate.Entities;

public sealed class TicketMessage : Entity<MessageId>
{
    public Guid AuthorId { get; private set; }
    public AuthorType AuthorType { get; private set; }
    public string MessageText { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public bool IsInternalNote { get; private set; }

    private TicketMessage(
        MessageId id,
        Guid authorId,
        AuthorType authorType,
        string messageText,
        DateTimeOffset timestamp,
        bool isInternalNote)
        : base(id)
    {
        AuthorId = authorId;
        AuthorType = authorType;
        MessageText = messageText;
        Timestamp = timestamp;
        IsInternalNote = isInternalNote;
    }

    public static Result<TicketMessage> Create(
        Guid authorId,
        AuthorType authorType,
        string messageText,
        bool isInternalNote = false)
    {
        // Validate author ID
        if (authorId == Guid.Empty)
        {
            return Result.Failure<TicketMessage>(SupportTicketErrors.InvalidAuthorId("Author ID cannot be empty"));
        }

        // Validate message text
        if (string.IsNullOrWhiteSpace(messageText))
        {
            return Result.Failure<TicketMessage>(SupportTicketErrors.InvalidMessageText("Message text cannot be empty"));
        }

        if (messageText.Length > 5000)
        {
            return Result.Failure<TicketMessage>(SupportTicketErrors.InvalidMessageText("Message text cannot exceed 5000 characters"));
        }

        var message = new TicketMessage(
            MessageId.CreateUnique(),
            authorId,
            authorType,
            messageText.Trim(),
            DateTimeOffset.UtcNow,
            isInternalNote);

        return Result.Success(message);
    }

    public static Result<TicketMessage> Create(
        MessageId id,
        Guid authorId,
        AuthorType authorType,
        string messageText,
        DateTimeOffset timestamp,
        bool isInternalNote = false)
    {
        // Validate author ID
        if (authorId == Guid.Empty)
        {
            return Result.Failure<TicketMessage>(SupportTicketErrors.InvalidAuthorId("Author ID cannot be empty"));
        }

        // Validate message text
        if (string.IsNullOrWhiteSpace(messageText))
        {
            return Result.Failure<TicketMessage>(SupportTicketErrors.InvalidMessageText("Message text cannot be empty"));
        }

        if (messageText.Length > 5000)
        {
            return Result.Failure<TicketMessage>(SupportTicketErrors.InvalidMessageText("Message text cannot exceed 5000 characters"));
        }

        var message = new TicketMessage(
            id,
            authorId,
            authorType,
            messageText.Trim(),
            timestamp,
            isInternalNote);

        return Result.Success(message);
    }

    // Messages are immutable once created - no update methods
    // This enforces the business rule that messages cannot be changed after creation

#pragma warning disable CS8618
    // For EF Core
    private TicketMessage()
    {
    }
#pragma warning restore CS8618
}
