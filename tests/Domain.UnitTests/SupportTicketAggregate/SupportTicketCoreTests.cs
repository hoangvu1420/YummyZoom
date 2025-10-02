using YummyZoom.Domain.SupportTicketAggregate;
using YummyZoom.Domain.SupportTicketAggregate.Entities;
using YummyZoom.Domain.SupportTicketAggregate.Enums;
using YummyZoom.Domain.SupportTicketAggregate.Errors;
using YummyZoom.Domain.SupportTicketAggregate.Events;
using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.SupportTicketAggregate;

/// <summary>
/// Tests for core SupportTicket aggregate functionality including creation and lifecycle management.
/// </summary>
[TestFixture]
public class SupportTicketCoreTests
{
    private static readonly Guid DefaultAuthorId = Guid.NewGuid();
    private const AuthorType DefaultAuthorType = AuthorType.Customer;
    private const string DefaultSubject = "Test support ticket";
    private const SupportTicketType DefaultType = SupportTicketType.GeneralInquiry;
    private const SupportTicketPriority DefaultPriority = SupportTicketPriority.Normal;
    private const string DefaultInitialMessage = "This is a test support ticket message";
    private const int DefaultTicketSequenceNumber = 12345;

    #region Helper Methods

    private static ContextLink CreateDefaultContextLink()
    {
        var result = ContextLink.Create(ContextEntityType.User, Guid.NewGuid());
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static List<ContextLink> CreateDefaultContextLinks()
    {
        return [CreateDefaultContextLink()];
    }

    #endregion

    #region Create() Method Tests - New Ticket

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeTicketCorrectly()
    {
        // Arrange
        var contextLinks = CreateDefaultContextLinks();

        // Act
        var result = SupportTicket.Create(
            DefaultSubject,
            DefaultType,
            DefaultPriority,
            contextLinks,
            DefaultInitialMessage,
            DefaultAuthorId,
            DefaultAuthorType,
            DefaultTicketSequenceNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var ticket = result.Value;

        ticket.Id.Value.Should().NotBe(Guid.Empty, "because a unique SupportTicketId should be generated");
        ticket.TicketNumber.Value.Should().StartWith("TKT-", "because ticket numbers should follow the expected format");
        ticket.Subject.Should().Be(DefaultSubject);
        ticket.Status.Should().Be(SupportTicketStatus.Open, "because new tickets are created as Open");
        ticket.Type.Should().Be(DefaultType);
        ticket.Priority.Should().Be(DefaultPriority);
        ticket.AssignedToAdminId.Should().BeNull("because new tickets are not assigned by default");
        ticket.ContextLinks.Should().HaveCount(1);
        ticket.Messages.Should().HaveCount(1, "because initial message should be added");
        ticket.SubmissionTimestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        ticket.LastUpdateTimestamp.Should().Be(ticket.SubmissionTimestamp);
    }

    [Test]
    public void Create_WithValidInputs_ShouldRaiseDomainEvents()
    {
        // Arrange
        var contextLinks = CreateDefaultContextLinks();

        // Act
        var result = SupportTicket.Create(
            DefaultSubject,
            DefaultType,
            DefaultPriority,
            contextLinks,
            DefaultInitialMessage,
            DefaultAuthorId,
            DefaultAuthorType,
            DefaultTicketSequenceNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var ticket = result.Value;

        ticket.DomainEvents.Should().HaveCount(2);
        ticket.DomainEvents.Should().ContainItemsAssignableTo<SupportTicketCreated>();
        ticket.DomainEvents.Should().ContainItemsAssignableTo<TicketMessageAdded>();

        var createdEvent = ticket.DomainEvents.OfType<SupportTicketCreated>().First();
        createdEvent.SupportTicketId.Should().Be(ticket.Id);
        createdEvent.Subject.Should().Be(DefaultSubject);
        createdEvent.Type.Should().Be(DefaultType);
        createdEvent.Priority.Should().Be(DefaultPriority);

        var messageEvent = ticket.DomainEvents.OfType<TicketMessageAdded>().First();
        messageEvent.SupportTicketId.Should().Be(ticket.Id);
        messageEvent.AuthorId.Should().Be(DefaultAuthorId);
        messageEvent.AuthorType.Should().Be(DefaultAuthorType);
        messageEvent.MessageText.Should().Be(DefaultInitialMessage);
        messageEvent.IsInternalNote.Should().BeFalse();
    }

    [Test]
    public void Create_WithEmptySubject_ShouldFailWithValidationError()
    {
        // Arrange
        var contextLinks = CreateDefaultContextLinks();

        // Act
        var result = SupportTicket.Create(
            "",
            DefaultType,
            DefaultPriority,
            contextLinks,
            DefaultInitialMessage,
            DefaultAuthorId,
            DefaultAuthorType,
            DefaultTicketSequenceNumber);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.InvalidSubject("Subject cannot be empty"));
    }

    [Test]
    public void Create_WithNullSubject_ShouldFailWithValidationError()
    {
        // Arrange
        var contextLinks = CreateDefaultContextLinks();

        // Act
        var result = SupportTicket.Create(
            null!,
            DefaultType,
            DefaultPriority,
            contextLinks,
            DefaultInitialMessage,
            DefaultAuthorId,
            DefaultAuthorType,
            DefaultTicketSequenceNumber);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.InvalidSubject("Subject cannot be empty"));
    }

    [Test]
    public void Create_WithSubjectTooLong_ShouldFailWithValidationError()
    {
        // Arrange
        var contextLinks = CreateDefaultContextLinks();
        var longSubject = new string('A', 201); // Exceeds 200 character limit

        // Act
        var result = SupportTicket.Create(
            longSubject,
            DefaultType,
            DefaultPriority,
            contextLinks,
            DefaultInitialMessage,
            DefaultAuthorId,
            DefaultAuthorType,
            DefaultTicketSequenceNumber);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.InvalidSubject("Subject cannot exceed 200 characters"));
    }

    [Test]
    public void Create_WithMaxLengthSubject_ShouldSucceed()
    {
        // Arrange
        var contextLinks = CreateDefaultContextLinks();
        var maxLengthSubject = new string('A', 200); // Exactly 200 characters

        // Act
        var result = SupportTicket.Create(
            maxLengthSubject,
            DefaultType,
            DefaultPriority,
            contextLinks,
            DefaultInitialMessage,
            DefaultAuthorId,
            DefaultAuthorType,
            DefaultTicketSequenceNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Subject.Should().Be(maxLengthSubject);
    }

    [Test]
    public void Create_WithNoContextLinks_ShouldFailWithValidationError()
    {
        // Arrange
        var emptyContextLinks = new List<ContextLink>();

        // Act
        var result = SupportTicket.Create(
            DefaultSubject,
            DefaultType,
            DefaultPriority,
            emptyContextLinks,
            DefaultInitialMessage,
            DefaultAuthorId,
            DefaultAuthorType,
            DefaultTicketSequenceNumber);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.NoContextLinksProvided);
    }

    [Test]
    public void Create_WithNullContextLinks_ShouldFailWithValidationError()
    {
        // Act
        var result = SupportTicket.Create(
            DefaultSubject,
            DefaultType,
            DefaultPriority,
            null!,
            DefaultInitialMessage,
            DefaultAuthorId,
            DefaultAuthorType,
            DefaultTicketSequenceNumber);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.NoContextLinksProvided);
    }

    [Test]
    public void Create_WithInvalidInitialMessage_ShouldFailWithValidationError()
    {
        // Arrange
        var contextLinks = CreateDefaultContextLinks();

        // Act
        var result = SupportTicket.Create(
            DefaultSubject,
            DefaultType,
            DefaultPriority,
            contextLinks,
            "", // Empty message
            DefaultAuthorId,
            DefaultAuthorType,
            DefaultTicketSequenceNumber);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.InvalidMessageText("Message text cannot be empty"));
    }

    [Test]
    public void Create_WithEmptyAuthorId_ShouldFailWithValidationError()
    {
        // Arrange
        var contextLinks = CreateDefaultContextLinks();

        // Act
        var result = SupportTicket.Create(
            DefaultSubject,
            DefaultType,
            DefaultPriority,
            contextLinks,
            DefaultInitialMessage,
            Guid.Empty,
            DefaultAuthorType,
            DefaultTicketSequenceNumber);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.InvalidAuthorId("Author ID cannot be empty"));
    }

    [Test]
    public void Create_WithMultipleContextLinks_ShouldIncludeAllContextLinks()
    {
        // Arrange
        var contextLinks = new List<ContextLink>
        {
            ContextLink.Create(ContextEntityType.User, Guid.NewGuid()).Value,
            ContextLink.Create(ContextEntityType.Order, Guid.NewGuid()).Value,
            ContextLink.Create(ContextEntityType.Restaurant, Guid.NewGuid()).Value
        };

        // Act
        var result = SupportTicket.Create(
            DefaultSubject,
            DefaultType,
            DefaultPriority,
            contextLinks,
            DefaultInitialMessage,
            DefaultAuthorId,
            DefaultAuthorType,
            DefaultTicketSequenceNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var ticket = result.Value;
        ticket.ContextLinks.Should().HaveCount(3);
        ticket.ContextLinks.Should().Contain(contextLinks);
    }

    [Test]
    public void Create_WithWhitespaceSubject_ShouldTrimAndSucceed()
    {
        // Arrange
        var contextLinks = CreateDefaultContextLinks();
        var subjectWithWhitespace = "  Test Subject  ";

        // Act
        var result = SupportTicket.Create(
            subjectWithWhitespace,
            DefaultType,
            DefaultPriority,
            contextLinks,
            DefaultInitialMessage,
            DefaultAuthorId,
            DefaultAuthorType,
            DefaultTicketSequenceNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Subject.Should().Be("Test Subject");
    }

    #endregion

    #region Create() Method Tests - Reconstruction

    [Test]
    public void Create_ForReconstruction_WithValidInputs_ShouldSucceed()
    {
        // Arrange
        var ticketId = SupportTicketId.CreateUnique();
        var ticketNumber = TicketNumber.CreateFromSequence(123);
        var contextLinks = CreateDefaultContextLinks();
        var messages = new List<TicketMessage>
        {
            TicketMessage.Create(DefaultAuthorId, DefaultAuthorType, DefaultInitialMessage).Value
        };
        var submissionTime = DateTimeOffset.UtcNow.AddHours(-1);
        var lastUpdateTime = DateTimeOffset.UtcNow.AddMinutes(-30);
        var adminId = Guid.NewGuid();

        // Act
        var result = SupportTicket.Create(
            ticketId,
            ticketNumber,
            DefaultSubject,
            DefaultType,
            DefaultPriority,
            SupportTicketStatus.InProgress,
            submissionTime,
            lastUpdateTime,
            contextLinks,
            messages,
            adminId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var ticket = result.Value;

        ticket.Id.Should().Be(ticketId);
        ticket.TicketNumber.Should().Be(ticketNumber);
        ticket.Subject.Should().Be(DefaultSubject);
        ticket.Status.Should().Be(SupportTicketStatus.InProgress);
        ticket.SubmissionTimestamp.Should().Be(submissionTime);
        ticket.LastUpdateTimestamp.Should().Be(lastUpdateTime);
        ticket.AssignedToAdminId.Should().Be(adminId);
        ticket.ContextLinks.Should().HaveCount(1);
        ticket.Messages.Should().HaveCount(1);
    }

    [Test]
    public void Create_ForReconstruction_WithInvalidSubject_ShouldFail()
    {
        // Arrange
        var ticketId = SupportTicketId.CreateUnique();
        var ticketNumber = TicketNumber.CreateFromSequence(123);
        var contextLinks = CreateDefaultContextLinks();
        var messages = new List<TicketMessage>();

        // Act
        var result = SupportTicket.Create(
            ticketId,
            ticketNumber,
            "", // Empty subject
            DefaultType,
            DefaultPriority,
            SupportTicketStatus.Open,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            contextLinks,
            messages);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.InvalidSubject("Subject cannot be empty"));
    }

    [Test]
    public void Create_ForReconstruction_WithNoContextLinks_ShouldFail()
    {
        // Arrange
        var ticketId = SupportTicketId.CreateUnique();
        var ticketNumber = TicketNumber.CreateFromSequence(123);
        var emptyContextLinks = new List<ContextLink>();
        var messages = new List<TicketMessage>();

        // Act
        var result = SupportTicket.Create(
            ticketId,
            ticketNumber,
            DefaultSubject,
            DefaultType,
            DefaultPriority,
            SupportTicketStatus.Open,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            emptyContextLinks,
            messages);

        // Assert
        result.ShouldBeFailure();
        result.Error.Should().Be(SupportTicketErrors.NoContextLinksProvided);
    }

    [Test]
    public void Create_ForReconstruction_WithNullMessages_ShouldCreateEmptyMessageList()
    {
        // Arrange
        var ticketId = SupportTicketId.CreateUnique();
        var ticketNumber = TicketNumber.CreateFromSequence(123);
        var contextLinks = CreateDefaultContextLinks();

        // Act
        var result = SupportTicket.Create(
            ticketId,
            ticketNumber,
            DefaultSubject,
            DefaultType,
            DefaultPriority,
            SupportTicketStatus.Open,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            contextLinks,
            null!); // Null messages

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Messages.Should().BeEmpty();
    }

    #endregion

    #region Property Immutability Tests

    [Test]
    public void ContextLinks_ShouldBeReadOnly()
    {
        // Arrange
        var contextLinks = new List<ContextLink> { CreateDefaultContextLink() };

        // Act
        var ticket = SupportTicket.Create(
            SupportTicketId.CreateUnique(),
            TicketNumber.CreateFromSequence(123),
            DefaultSubject,
            DefaultType,
            DefaultPriority,
            SupportTicketStatus.Open,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            contextLinks,
            new List<TicketMessage>()).Value;

        // Assert
        // Type check
        var property = typeof(SupportTicket).GetProperty(nameof(SupportTicket.ContextLinks));
        property.Should().NotBeNull();
        typeof(IReadOnlyList<ContextLink>).IsAssignableFrom(property!.PropertyType).Should().BeTrue();

        // Immutability check: mutation should throw
        Action mutate = () => ((ICollection<ContextLink>)ticket.ContextLinks).Add(CreateDefaultContextLink());
        mutate.Should().Throw<NotSupportedException>();

        // Verify that modifying the original list doesn't affect the ticket
        contextLinks.Add(CreateDefaultContextLink());
        ticket.ContextLinks.Should().HaveCount(1);
    }

    [Test]
    public void Messages_ShouldBeReadOnly()
    {
        // Arrange
        var messages = new List<TicketMessage>
        {
            TicketMessage.Create(DefaultAuthorId, DefaultAuthorType, DefaultInitialMessage).Value
        };

        // Act
        var ticket = SupportTicket.Create(
            SupportTicketId.CreateUnique(),
            TicketNumber.CreateFromSequence(123),
            DefaultSubject,
            DefaultType,
            DefaultPriority,
            SupportTicketStatus.Open,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            CreateDefaultContextLinks(),
            messages).Value;

        // Assert
        // Type check
        var property = typeof(SupportTicket).GetProperty(nameof(SupportTicket.Messages));
        property.Should().NotBeNull();
        typeof(IReadOnlyList<TicketMessage>).IsAssignableFrom(property!.PropertyType).Should().BeTrue();

        // Immutability check: mutation should throw
        Action mutate = () => ((ICollection<TicketMessage>)ticket.Messages).Add(
            TicketMessage.Create(Guid.NewGuid(), AuthorType.Admin, "Another message").Value);
        mutate.Should().Throw<NotSupportedException>();

        // Verify that modifying the original list doesn't affect the ticket
        messages.Add(TicketMessage.Create(Guid.NewGuid(), AuthorType.Admin, "Another message").Value);
        ticket.Messages.Should().HaveCount(1);
    }

    #endregion
}
