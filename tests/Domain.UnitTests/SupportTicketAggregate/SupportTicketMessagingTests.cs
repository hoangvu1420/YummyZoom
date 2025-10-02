using YummyZoom.Domain.SupportTicketAggregate;
using YummyZoom.Domain.SupportTicketAggregate.Enums;
using YummyZoom.Domain.SupportTicketAggregate.Errors;
using YummyZoom.Domain.SupportTicketAggregate.Events;
using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.SupportTicketAggregate;

/// <summary>
/// Tests for SupportTicket messaging functionality including adding messages and message queries.
/// </summary>
[TestFixture]
public class SupportTicketMessagingTests
{
    private static readonly Guid DefaultAuthorId = Guid.NewGuid();
    private static readonly Guid DefaultAdminId = Guid.NewGuid();
    private const string DefaultMessage = "This is a test message";
    private const string DefaultInternalNote = "This is an internal admin note";

    #region Helper Methods

    private static SupportTicket CreateDefaultTicket(SupportTicketStatus status = SupportTicketStatus.Open)
    {
        var contextLinks = new List<ContextLink>
        {
            ContextLink.Create(ContextEntityType.User, Guid.NewGuid()).Value
        };

        var result = SupportTicket.Create(
            "Test Ticket",
            SupportTicketType.GeneralInquiry,
            SupportTicketPriority.Normal,
            contextLinks,
            "Initial message",
            DefaultAuthorId,
            AuthorType.Customer,
            12345);

        result.IsSuccess.Should().BeTrue();
        var ticket = result.Value;

        // Clear domain events from creation
        ticket.ClearDomainEvents();

        // Set status if different from Open
        if (status != SupportTicketStatus.Open)
        {
            // Follow valid status transitions
            if (status == SupportTicketStatus.PendingCustomerResponse)
            {
                // Open -> InProgress -> PendingCustomerResponse
                var inProgressResult = ticket.UpdateStatus(SupportTicketStatus.InProgress, DefaultAdminId);
                inProgressResult.IsSuccess.Should().BeTrue();
                ticket.ClearDomainEvents();

                var pendingResult = ticket.UpdateStatus(SupportTicketStatus.PendingCustomerResponse, DefaultAdminId);
                pendingResult.IsSuccess.Should().BeTrue();
                ticket.ClearDomainEvents();
            }
            else if (status == SupportTicketStatus.Resolved)
            {
                // Open -> InProgress -> Resolved
                var inProgressResult = ticket.UpdateStatus(SupportTicketStatus.InProgress, DefaultAdminId);
                inProgressResult.IsSuccess.Should().BeTrue();
                ticket.ClearDomainEvents();

                var resolvedResult = ticket.UpdateStatus(SupportTicketStatus.Resolved, DefaultAdminId);
                resolvedResult.IsSuccess.Should().BeTrue();
                ticket.ClearDomainEvents();
            }
            else if (status == SupportTicketStatus.Closed)
            {
                // Open -> Closed (direct transition allowed)
                var statusResult = ticket.UpdateStatus(status, DefaultAdminId);
                statusResult.IsSuccess.Should().BeTrue();
                ticket.ClearDomainEvents();
            }
            else
            {
                // For other statuses, try direct transition
                var statusResult = ticket.UpdateStatus(status, DefaultAdminId);
                statusResult.IsSuccess.Should().BeTrue();
                ticket.ClearDomainEvents();
            }
        }

        return ticket;
    }

    #endregion

    #region AddMessage() Tests

    [Test]
    public void AddMessage_WithValidPublicMessage_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var initialMessageCount = ticket.Messages.Count;

        // Act
        var result = ticket.AddMessage(DefaultAuthorId, AuthorType.Customer, DefaultMessage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Messages.Should().HaveCount(initialMessageCount + 1);
        ticket.LastUpdateTimestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        var newMessage = ticket.Messages.Last();
        newMessage.AuthorId.Should().Be(DefaultAuthorId);
        newMessage.AuthorType.Should().Be(AuthorType.Customer);
        newMessage.MessageText.Should().Be(DefaultMessage);
        newMessage.IsInternalNote.Should().BeFalse();
        newMessage.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public void AddMessage_WithValidPublicMessage_ShouldRaiseDomainEvent()
    {
        // Arrange
        var ticket = CreateDefaultTicket();

        // Act
        var result = ticket.AddMessage(DefaultAuthorId, AuthorType.Customer, DefaultMessage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.DomainEvents.Should().HaveCount(1);

        var messageEvent = ticket.DomainEvents.OfType<TicketMessageAdded>().Single();
        messageEvent.SupportTicketId.Should().Be(ticket.Id);
        messageEvent.AuthorId.Should().Be(DefaultAuthorId);
        messageEvent.AuthorType.Should().Be(AuthorType.Customer);
        messageEvent.MessageText.Should().Be(DefaultMessage);
        messageEvent.IsInternalNote.Should().BeFalse();
    }

    [Test]
    public void AddMessage_WithInternalNoteByAdmin_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateDefaultTicket();

        // Act
        var result = ticket.AddMessage(DefaultAdminId, AuthorType.Admin, DefaultInternalNote, isInternalNote: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var newMessage = ticket.Messages.Last();
        newMessage.IsInternalNote.Should().BeTrue();
        newMessage.AuthorType.Should().Be(AuthorType.Admin);
        newMessage.MessageText.Should().Be(DefaultInternalNote);
    }

    [Test]
    public void AddMessage_WithInternalNoteByNonAdmin_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket();

        // Act
        var result = ticket.AddMessage(DefaultAuthorId, AuthorType.Customer, DefaultInternalNote, isInternalNote: true);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.TicketUpdateFailed("Only admins can add internal notes"));
    }

    [Test]
    public void AddMessage_WithInternalNoteByRestaurantOwner_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket();

        // Act
        var result = ticket.AddMessage(DefaultAuthorId, AuthorType.RestaurantOwner, DefaultInternalNote, isInternalNote: true);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.TicketUpdateFailed("Only admins can add internal notes"));
    }

    [Test]
    public void AddMessage_ToClosedTicket_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.Closed);

        // Act
        var result = ticket.AddMessage(DefaultAuthorId, AuthorType.Customer, DefaultMessage);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.TicketUpdateFailed("Cannot add messages to a closed ticket"));
    }

    [Test]
    public void AddMessage_WithEmptyMessage_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket();

        // Act
        var result = ticket.AddMessage(DefaultAuthorId, AuthorType.Customer, "");

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.InvalidMessageText("Message text cannot be empty"));
    }

    [Test]
    public void AddMessage_WithEmptyAuthorId_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket();

        // Act
        var result = ticket.AddMessage(Guid.Empty, AuthorType.Customer, DefaultMessage);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.InvalidAuthorId("Author ID cannot be empty"));
    }

    [Test]
    public void AddMessage_WithMessageTooLong_ShouldFail()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var longMessage = new string('A', 5001); // Exceeds 5000 character limit

        // Act
        var result = ticket.AddMessage(DefaultAuthorId, AuthorType.Customer, longMessage);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.InvalidMessageText("Message text cannot exceed 5000 characters"));
    }

    [Test]
    public void AddMessage_CustomerResponseToPendingTicket_ShouldUpdateStatusToInProgress()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.PendingCustomerResponse);

        // Act
        var result = ticket.AddMessage(DefaultAuthorId, AuthorType.Customer, DefaultMessage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.InProgress, "because customer responded to pending ticket");
    }

    [Test]
    public void AddMessage_AdminResponseToPendingTicket_ShouldNotUpdateStatus()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.PendingCustomerResponse);

        // Act
        var result = ticket.AddMessage(DefaultAdminId, AuthorType.Admin, DefaultMessage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.PendingCustomerResponse, "because only customer responses change status");
    }

    [Test]
    public void AddMessage_RestaurantOwnerResponseToPendingTicket_ShouldNotUpdateStatus()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.PendingCustomerResponse);

        // Act
        var result = ticket.AddMessage(DefaultAuthorId, AuthorType.RestaurantOwner, DefaultMessage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.PendingCustomerResponse, "because only customer responses change status");
    }

    [Test]
    public void AddMessage_CustomerResponseToNonPendingTicket_ShouldNotUpdateStatus()
    {
        // Arrange
        var ticket = CreateDefaultTicket(SupportTicketStatus.InProgress);

        // Act
        var result = ticket.AddMessage(DefaultAuthorId, AuthorType.Customer, DefaultMessage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(SupportTicketStatus.InProgress, "because status only changes from PendingCustomerResponse");
    }

    #endregion

    #region Message Query Tests

    [Test]
    public void GetPublicMessages_ShouldReturnOnlyNonInternalMessages()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        ticket.AddMessage(DefaultAuthorId, AuthorType.Customer, "Public message 1");
        ticket.AddMessage(DefaultAdminId, AuthorType.Admin, "Internal note", isInternalNote: true);
        ticket.AddMessage(DefaultAuthorId, AuthorType.Customer, "Public message 2");

        // Act
        var publicMessages = ticket.GetPublicMessages();

        // Assert
        publicMessages.Should().HaveCount(3); // Initial message + 2 public messages
        publicMessages.Should().OnlyContain(m => !m.IsInternalNote);
        publicMessages.Should().Contain(m => m.MessageText == "Public message 1");
        publicMessages.Should().Contain(m => m.MessageText == "Public message 2");
        publicMessages.Should().NotContain(m => m.MessageText == "Internal note");
    }

    [Test]
    public void GetInternalNotes_ShouldReturnOnlyInternalMessages()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        ticket.AddMessage(DefaultAuthorId, AuthorType.Customer, "Public message");
        ticket.AddMessage(DefaultAdminId, AuthorType.Admin, "Internal note 1", isInternalNote: true);
        ticket.AddMessage(DefaultAdminId, AuthorType.Admin, "Internal note 2", isInternalNote: true);

        // Act
        var internalNotes = ticket.GetInternalNotes();

        // Assert
        internalNotes.Should().HaveCount(2);
        internalNotes.Should().OnlyContain(m => m.IsInternalNote);
        internalNotes.Should().Contain(m => m.MessageText == "Internal note 1");
        internalNotes.Should().Contain(m => m.MessageText == "Internal note 2");
        internalNotes.Should().NotContain(m => m.MessageText == "Public message");
    }

    [Test]
    public void GetLatestMessage_ShouldReturnMostRecentMessage()
    {
        // Arrange
        var ticket = CreateDefaultTicket();

        // Add messages with slight delays to ensure different timestamps
        ticket.AddMessage(DefaultAuthorId, AuthorType.Customer, "First message");
        Thread.Sleep(1); // Ensure different timestamp
        ticket.AddMessage(DefaultAdminId, AuthorType.Admin, "Second message");
        Thread.Sleep(1); // Ensure different timestamp
        ticket.AddMessage(DefaultAuthorId, AuthorType.Customer, "Latest message");

        // Act
        var latestMessage = ticket.GetLatestMessage();

        // Assert
        latestMessage.Should().NotBeNull();
        latestMessage!.MessageText.Should().Be("Latest message");
    }

    [Test]
    public void GetLatestMessage_WithOnlyInitialMessage_ShouldReturnInitialMessage()
    {
        // Arrange
        var ticket = CreateDefaultTicket();

        // Act
        var latestMessage = ticket.GetLatestMessage();

        // Assert
        latestMessage.Should().NotBeNull();
        latestMessage!.MessageText.Should().Be("Initial message");
    }

    [Test]
    public void GetMessageCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var ticket = CreateDefaultTicket();
        var initialCount = ticket.GetMessageCount();

        // Act & Assert - Add messages and verify count increases
        ticket.AddMessage(DefaultAuthorId, AuthorType.Customer, "Message 1");
        ticket.GetMessageCount().Should().Be(initialCount + 1);

        ticket.AddMessage(DefaultAdminId, AuthorType.Admin, "Internal note", isInternalNote: true);
        ticket.GetMessageCount().Should().Be(initialCount + 2);

        ticket.AddMessage(DefaultAuthorId, AuthorType.Customer, "Message 2");
        ticket.GetMessageCount().Should().Be(initialCount + 3);
    }

    [Test]
    public void GetMessageCount_ForNewTicket_ShouldReturnOne()
    {
        // Arrange & Act
        var ticket = CreateDefaultTicket();

        // Assert
        ticket.GetMessageCount().Should().Be(1, "because new tickets have an initial message");
    }

    #endregion
}
