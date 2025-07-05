using YummyZoom.Domain.SupportTicketAggregate.Entities;
using YummyZoom.Domain.SupportTicketAggregate.Enums;
using YummyZoom.Domain.SupportTicketAggregate.Errors;
using YummyZoom.Domain.SupportTicketAggregate.Events;
using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.SupportTicketAggregate;

public sealed class SupportTicket : AggregateRoot<SupportTicketId, Guid>
{
    private readonly List<ContextLink> _contextLinks = [];
    private readonly List<TicketMessage> _messages = [];

    public TicketNumber TicketNumber { get; private set; }
    public string Subject { get; private set; }
    public SupportTicketStatus Status { get; private set; }
    public SupportTicketPriority Priority { get; private set; }
    public SupportTicketType Type { get; private set; }
    public DateTimeOffset SubmissionTimestamp { get; private set; }
    public DateTimeOffset LastUpdateTimestamp { get; private set; }
    public Guid? AssignedToAdminId { get; private set; }

    public IReadOnlyList<ContextLink> ContextLinks => _contextLinks.AsReadOnly();
    public IReadOnlyList<TicketMessage> Messages => _messages.AsReadOnly();

    private SupportTicket(
        SupportTicketId id,
        TicketNumber ticketNumber,
        string subject,
        SupportTicketType type,
        SupportTicketPriority priority,
        DateTimeOffset submissionTimestamp,
        List<ContextLink> contextLinks,
        List<TicketMessage> messages,
        SupportTicketStatus status = SupportTicketStatus.Open,
        Guid? assignedToAdminId = null)
        : base(id)
    {
        TicketNumber = ticketNumber;
        Subject = subject;
        Type = type;
        Priority = priority;
        Status = status;
        SubmissionTimestamp = submissionTimestamp;
        LastUpdateTimestamp = submissionTimestamp;
        AssignedToAdminId = assignedToAdminId;
        _contextLinks = new List<ContextLink>(contextLinks);
        _messages = new List<TicketMessage>(messages);
    }

    public static Result<SupportTicket> Create(
        string subject,
        SupportTicketType type,
        SupportTicketPriority priority,
        IReadOnlyList<ContextLink> contextLinks,
        string initialMessage,
        Guid authorId,
        AuthorType authorType,
        int ticketSequenceNumber)
    {
        // Validate subject
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result.Failure<SupportTicket>(SupportTicketErrors.InvalidSubject("Subject cannot be empty"));
        }

        if (subject.Length > 200)
        {
            return Result.Failure<SupportTicket>(SupportTicketErrors.InvalidSubject("Subject cannot exceed 200 characters"));
        }

        // Validate context links
        if (contextLinks == null || contextLinks.Count == 0)
        {
            return Result.Failure<SupportTicket>(SupportTicketErrors.NoContextLinksProvided);
        }

        // Create initial message
        var messageResult = TicketMessage.Create(authorId, authorType, initialMessage);
        if (messageResult.IsFailure)
        {
            return Result.Failure<SupportTicket>(messageResult.Error);
        }

        var ticketId = SupportTicketId.CreateUnique();
        var ticketNumber = TicketNumber.CreateFromSequence(ticketSequenceNumber);
        var now = DateTimeOffset.UtcNow;

        var ticket = new SupportTicket(
            ticketId,
            ticketNumber,
            subject.Trim(),
            type,
            priority,
            now,
            contextLinks.ToList(),
            [messageResult.Value],
            SupportTicketStatus.Open);

        // Add domain event
        ticket.AddDomainEvent(new SupportTicketCreated(
            ticketId,
            ticketNumber,
            subject.Trim(),
            type,
            priority,
            contextLinks));

        // Add domain event for the initial message
        ticket.AddDomainEvent(new TicketMessageAdded(
            ticketId,
            messageResult.Value.Id,
            authorId,
            authorType,
            initialMessage,
            false));

        return Result.Success(ticket);
    }

    public static Result<SupportTicket> Create(
        SupportTicketId id,
        TicketNumber ticketNumber,
        string subject,
        SupportTicketType type,
        SupportTicketPriority priority,
        SupportTicketStatus status,
        DateTimeOffset submissionTimestamp,
        DateTimeOffset lastUpdateTimestamp,
        IReadOnlyList<ContextLink> contextLinks,
        IReadOnlyList<TicketMessage> messages,
        Guid? assignedToAdminId = null)
    {
        // Validation for reconstruction
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result.Failure<SupportTicket>(SupportTicketErrors.InvalidSubject("Subject cannot be empty"));
        }

        if (contextLinks == null || contextLinks.Count == 0)
        {
            return Result.Failure<SupportTicket>(SupportTicketErrors.NoContextLinksProvided);
        }

        var ticket = new SupportTicket(
            id,
            ticketNumber,
            subject,
            type,
            priority,
            submissionTimestamp,
            contextLinks.ToList(),
            messages?.ToList() ?? [],
            status,
            assignedToAdminId);

        ticket.LastUpdateTimestamp = lastUpdateTimestamp;

        return Result.Success(ticket);
    }

    public Result AddMessage(Guid authorId, AuthorType authorType, string messageText, bool isInternalNote = false)
    {
        // Check if messages can be added to this ticket
        if (!CanAddMessage())
        {
            return Result.Failure(SupportTicketErrors.TicketUpdateFailed("Cannot add messages to a closed ticket"));
        }

        // Validate that internal notes can only be added by admins
        if (isInternalNote && authorType != AuthorType.Admin)
        {
            return Result.Failure(SupportTicketErrors.TicketUpdateFailed("Only admins can add internal notes"));
        }

        var messageResult = TicketMessage.Create(authorId, authorType, messageText, isInternalNote);
        if (messageResult.IsFailure)
        {
            return Result.Failure(messageResult.Error);
        }

        _messages.Add(messageResult.Value);
        LastUpdateTimestamp = DateTimeOffset.UtcNow;

        // Auto-update status based on author type if appropriate
        if (authorType == AuthorType.Customer && Status == SupportTicketStatus.PendingCustomerResponse)
        {
            // Customer responded, move back to InProgress
            Status = SupportTicketStatus.InProgress;
        }

        // Add domain event
        AddDomainEvent(new TicketMessageAdded(
            (SupportTicketId)Id,
            messageResult.Value.Id,
            authorId,
            authorType,
            messageText,
            isInternalNote));

        return Result.Success();
    }

    public Result UpdateStatus(SupportTicketStatus newStatus, Guid? adminId = null)
    {
        // Validate that the status is changing
        if (Status == newStatus)
        {
            return Result.Failure(SupportTicketErrors.TicketUpdateFailed($"Ticket is already in '{newStatus}' status"));
        }

        // Validate status transition
        if (!IsValidStatusTransition(Status, newStatus))
        {
            return Result.Failure(SupportTicketErrors.InvalidStatusTransition(Status.ToString(), newStatus.ToString()));
        }

        // Check if admin authorization is required
        if (RequiresAdminForStatusChange(newStatus) && adminId == null)
        {
            return Result.Failure(SupportTicketErrors.UnauthorizedStatusChange(newStatus.ToString()));
        }

        // Additional business rule: Can't change status if ticket is closed (except for resolved -> closed)
        if (!CanChangeStatus() && !(Status == SupportTicketStatus.Resolved && newStatus == SupportTicketStatus.Closed))
        {
            return Result.Failure(SupportTicketErrors.InvalidStatusTransition("Closed", newStatus.ToString()));
        }

        var previousStatus = Status;
        Status = newStatus;
        LastUpdateTimestamp = DateTimeOffset.UtcNow;

        // Auto-assign to admin if moving to InProgress and not assigned
        if (newStatus == SupportTicketStatus.InProgress && AssignedToAdminId == null && adminId.HasValue)
        {
            AssignedToAdminId = adminId.Value;
        }

        // Add domain event
        AddDomainEvent(new SupportTicketStatusChanged(
            (SupportTicketId)Id,
            previousStatus,
            newStatus,
            adminId));

        return Result.Success();
    }

    public Result AssignToAdmin(Guid adminId)
    {
        if (adminId == Guid.Empty)
        {
            return Result.Failure(SupportTicketErrors.InvalidAdminId("Admin ID cannot be empty"));
        }

        var previousAdminId = AssignedToAdminId;
        AssignedToAdminId = adminId;
        LastUpdateTimestamp = DateTimeOffset.UtcNow;

        // Add domain event
        AddDomainEvent(new SupportTicketAssigned(
            (SupportTicketId)Id,
            adminId,
            previousAdminId));

        return Result.Success();
    }

    public Result UpdatePriority(SupportTicketPriority newPriority, Guid adminId)
    {
        if (adminId == Guid.Empty)
        {
            return Result.Failure(SupportTicketErrors.InvalidAdminId("Admin ID cannot be empty"));
        }

        var previousPriority = Priority;
        Priority = newPriority;
        LastUpdateTimestamp = DateTimeOffset.UtcNow;

        // Add domain event
        AddDomainEvent(new SupportTicketPriorityChanged(
            (SupportTicketId)Id,
            previousPriority,
            newPriority,
            adminId));

        return Result.Success();
    }

    public Result UnassignFromAdmin(Guid adminId)
    {
        if (adminId == Guid.Empty)
        {
            return Result.Failure(SupportTicketErrors.InvalidAdminId("Admin ID cannot be empty"));
        }

        if (AssignedToAdminId == null)
        {
            return Result.Failure(SupportTicketErrors.TicketUpdateFailed("Ticket is not currently assigned to any admin"));
        }

        var previousAdminId = AssignedToAdminId;
        AssignedToAdminId = null;
        LastUpdateTimestamp = DateTimeOffset.UtcNow;

        // Add domain event
        AddDomainEvent(new SupportTicketAssigned(
            (SupportTicketId)Id,
            adminId, // The admin who performed the unassignment
            previousAdminId));

        return Result.Success();
    }

    public Result UpdateSubject(string newSubject, Guid adminId)
    {
        if (adminId == Guid.Empty)
        {
            return Result.Failure(SupportTicketErrors.InvalidAdminId("Admin ID cannot be empty"));
        }

        if (string.IsNullOrWhiteSpace(newSubject))
        {
            return Result.Failure(SupportTicketErrors.InvalidSubject("Subject cannot be empty"));
        }

        if (newSubject.Length > 200)
        {
            return Result.Failure(SupportTicketErrors.InvalidSubject("Subject cannot exceed 200 characters"));
        }

        Subject = newSubject.Trim();
        LastUpdateTimestamp = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    public Result AddContextLink(ContextLink contextLink, Guid adminId)
    {
        if (adminId == Guid.Empty)
        {
            return Result.Failure(SupportTicketErrors.InvalidAdminId("Admin ID cannot be empty"));
        }

        // Check if context link already exists
        if (_contextLinks.Any(cl => cl.EntityType == contextLink.EntityType && cl.EntityID == contextLink.EntityID))
        {
            return Result.Failure(SupportTicketErrors.TicketUpdateFailed("Context link already exists"));
        }

        _contextLinks.Add(contextLink);
        LastUpdateTimestamp = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    public Result RemoveContextLink(ContextEntityType entityType, Guid entityId, Guid adminId)
    {
        if (adminId == Guid.Empty)
        {
            return Result.Failure(SupportTicketErrors.InvalidAdminId("Admin ID cannot be empty"));
        }

        var contextLinkToRemove = _contextLinks.FirstOrDefault(cl => cl.EntityType == entityType && cl.EntityID == entityId);
        if (contextLinkToRemove is null)
        {
            return Result.Failure(SupportTicketErrors.TicketUpdateFailed("Context link not found"));
        }

        // Ensure at least one context link remains
        if (_contextLinks.Count <= 1)
        {
            return Result.Failure(SupportTicketErrors.NoContextLinksProvided);
        }

        _contextLinks.Remove(contextLinkToRemove);
        LastUpdateTimestamp = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    // Query methods for business logic
    public bool IsAssignedToAdmin(Guid adminId)
    {
        return AssignedToAdminId == adminId;
    }

    public bool IsOpen()
    {
        return Status == SupportTicketStatus.Open;
    }

    public bool IsClosed()
    {
        return Status == SupportTicketStatus.Closed;
    }

    public bool IsResolved()
    {
        return Status == SupportTicketStatus.Resolved;
    }

    public bool RequiresCustomerResponse()
    {
        return Status == SupportTicketStatus.PendingCustomerResponse;
    }

    public bool IsHighPriority()
    {
        return Priority is SupportTicketPriority.High or SupportTicketPriority.Urgent;
    }

    public bool HasContextLinkForEntity(ContextEntityType entityType, Guid entityId)
    {
        return _contextLinks.Any(cl => cl.EntityType == entityType && cl.EntityID == entityId);
    }

    public IReadOnlyList<TicketMessage> GetPublicMessages()
    {
        return _messages.Where(m => !m.IsInternalNote).ToList().AsReadOnly();
    }

    public IReadOnlyList<TicketMessage> GetInternalNotes()
    {
        return _messages.Where(m => m.IsInternalNote).ToList().AsReadOnly();
    }

    public TicketMessage? GetLatestMessage()
    {
        return _messages.OrderByDescending(m => m.Timestamp).FirstOrDefault();
    }

    public int GetMessageCount()
    {
        return _messages.Count;
    }

    public TimeSpan GetAge()
    {
        return DateTimeOffset.UtcNow - SubmissionTimestamp;
    }

    public TimeSpan GetTimeSinceLastUpdate()
    {
        return DateTimeOffset.UtcNow - LastUpdateTimestamp;
    }

    // Enhanced validation methods
    public static bool IsValidPriorityEscalation(SupportTicketPriority currentPriority, SupportTicketPriority newPriority)
    {
        // Define priority hierarchy (higher number = higher priority)
        var priorities = new Dictionary<SupportTicketPriority, int>
        {
            { SupportTicketPriority.Low, 1 },
            { SupportTicketPriority.Normal, 2 },
            { SupportTicketPriority.High, 3 },
            { SupportTicketPriority.Urgent, 4 }
        };

        return priorities[newPriority] >= priorities[currentPriority];
    }

    public bool CanBeAssignedToAdmin()
    {
        return !IsClosed();
    }

    public bool CanAddMessage()
    {
        return !IsClosed();
    }

    public bool CanChangeStatus()
    {
        return !IsClosed() || Status == SupportTicketStatus.Resolved; // Can reopen resolved tickets
    }

    private static bool RequiresAdminForStatusChange(SupportTicketStatus newStatus)
    {
        return newStatus is SupportTicketStatus.Resolved or SupportTicketStatus.Closed;
    }

    // Enhanced business rule validation
    private static bool IsValidTicketType(SupportTicketType type)
    {
        return Enum.IsDefined(typeof(SupportTicketType), type);
    }

    private static bool IsValidTicketPriority(SupportTicketPriority priority)
    {
        return Enum.IsDefined(typeof(SupportTicketPriority), priority);
    }

    private static bool IsValidAuthorType(AuthorType authorType)
    {
        return Enum.IsDefined(typeof(AuthorType), authorType);
    }

    public static bool IsValidSubjectLength(string subject)
    {
        return !string.IsNullOrWhiteSpace(subject) && subject.Length <= 200;
    }

    public static bool IsValidMessageLength(string message)
    {
        return !string.IsNullOrWhiteSpace(message) && message.Length <= 5000;
    }

    // Business rule: Determine if a ticket should be auto-escalated based on age and priority
    public bool ShouldAutoEscalate(TimeSpan maxAgeForPriority)
    {
        return GetAge() > maxAgeForPriority && 
               !IsHighPriority() && 
               !IsClosed() && 
               !IsResolved();
    }

    // Business rule: Determine if a ticket is stale (no recent activity)
    public bool IsStale(TimeSpan maxTimeSinceLastUpdate)
    {
        return GetTimeSinceLastUpdate() > maxTimeSinceLastUpdate && 
               !IsClosed() && 
               !IsResolved();
    }

    // Business rule: Check if ticket needs admin attention
    public bool NeedsAdminAttention()
    {
        return (IsOpen() || Status == SupportTicketStatus.PendingCustomerResponse) && 
               AssignedToAdminId == null;
    }

    // Business rule: Check if customer can respond
    public bool CanCustomerRespond()
    {
        return !IsClosed() && Status != SupportTicketStatus.Resolved;
    }

    // Business rule: Check if ticket is in a final state
    public bool IsInFinalState()
    {
        return IsClosed();
    }

    // Business rule: Get tickets that match specific context
    public bool IsRelatedToEntity(ContextEntityType entityType, Guid entityId)
    {
        return HasContextLinkForEntity(entityType, entityId);
    }

    // Business rule: Check if ticket type matches expected escalation path
    public bool IsEscalationCandidate()
    {
        return Type is SupportTicketType.RefundRequest or SupportTicketType.AccountIssue &&
               Priority == SupportTicketPriority.Low &&
               GetAge() > TimeSpan.FromDays(1);
    }

    private static bool IsValidStatusTransition(SupportTicketStatus currentStatus, SupportTicketStatus newStatus)
    {
        // Define valid status transitions
        return currentStatus switch
        {
            SupportTicketStatus.Open => newStatus is SupportTicketStatus.InProgress or SupportTicketStatus.Closed,
            SupportTicketStatus.InProgress => newStatus is SupportTicketStatus.PendingCustomerResponse or SupportTicketStatus.Resolved or SupportTicketStatus.Closed,
            SupportTicketStatus.PendingCustomerResponse => newStatus is SupportTicketStatus.InProgress or SupportTicketStatus.Closed,
            SupportTicketStatus.Resolved => newStatus is SupportTicketStatus.Closed or SupportTicketStatus.InProgress,
            SupportTicketStatus.Closed => false, // Closed tickets cannot be reopened
            _ => false
        };
    }

#pragma warning disable CS8618
    // For EF Core
    private SupportTicket()
    {
    }
#pragma warning restore CS8618
}
